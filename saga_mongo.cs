using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Saga.Infrastructure.Persistence.Context;
using SCore.Framework.Infrastructure.Job.OutboxInBox;
using SCore.Framework.Infrastructure.ServiceRegistration;

namespace Saga.Infrastructure.ServiceRegistration
{



    public static class ServiceExtention
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {

            #region ef

            //services.AddDbContext<SagaContext>(s => s.UseSqlServer(configuration.GetConnectionString("Sagadb")));

            services.AddDbContextPool<SagaContext>(s => s.UseSqlServer(configuration.GetConnectionString("Sagadb")), poolSize: 2048);
            //services.AddPooledDbContextFactory<SagaContext>(s => s.UseSqlServer(configuration.GetConnectionString("Sagadb")));

            #endregion

            //services.AddKeyedScoped<ISagaStateService, SagaStateServiceEF>("ef");
            //services.AddKeyedScoped<ISagaStateService, SagaStateService>("mongo");

            //services.AddScoped<ISagaStateService, SagaStateService>();

            //services.AddScoped<ISagaRepository, SagaStatesRepository>();


            //// ثبت IDataProcessor اصلی با استفاده از Factory Method
            //services.AddScoped<ISagaStateService>(serviceProvider =>
            //{
            //    // خواندن مقدار مورد نظر از فایل پیکربندی در زمان اجرا
            //    var dbProvider = configuration.GetSection("dbProvider").Value.ToLower();
            //    // بازگرداندن سرویس کلیددار متناظر
            //    return serviceProvider.GetRequiredKeyedService<ISagaStateService>(dbProvider);
            //});

            // ...

            //services.AddHostedService<JobOutboxProcessor>();
            //services.AddHostedService<JobSubscribeEvent>();
            //services.AddHostedService<JobInboxProcessor>();

            #region mongo

            string? connectionstring = configuration.GetConnectionString("connectionStringdbMongo");
            // 1. IMongoClient را به عنوان یک Singleton ثبت کنید
            services.AddSingleton<IMongoClient>(sp => new MongoClient(connectionstring));

            // 2. SagaDbContext را پیکربندی کنید (با استفاده از متد Factory)
            services.AddDbContext<SagaMongoDbContext>(options =>
             {
                 // دریافت IMongoClient ثبت شده از ServiceProvider
                 IMongoClient mongoClient = services.BuildServiceProvider().GetRequiredService<IMongoClient>();

                 
                 string dbName = "databaseNameMongo";
                 // ساخت IMongoDatabase
                 IMongoDatabase mongoDatabase = mongoClient.GetDatabase(dbName);

                 // استفاده از متد صحیح UseMongoDB که IMongoDatabase را می پذیرد
                 options.UseMongoDB(connectionstring, dbName);

             });

            #endregion

            services.AddOutbox(SCore.Framework.BuildingBlocks.Ioc.Extensions.GetAssemblies(["SCore"]));

            return services;
        }

    }

}


--------------------

using Newtonsoft.Json;
using Saga.Core.Contracts;
using Saga.Core.Entites;
using Saga.Infrastructure.Persistence.Context;
using Saga.Infrastructure.Repositories;
using SCore.Framework.BuildingBlocks.OutboxInbox;
using SCore.Shared.Event;
using Stateless;

namespace Saga.Endpoint.Orchestrators
{
    public abstract class BaseSagaOrchestrator<TState, TTrigger> : ISaga<TState, TTrigger>
        where TState : struct, Enum
        where TTrigger : struct, Enum
    {
        private readonly ISagaRepository _sagaRepository;
        private readonly IOutboxRepository _outboxRepository;
        private readonly ISagaUnitOfWork _sagaUnitOfWork;
        private readonly ISagaStateHistoryRepository _sagaStateHistoryRepository;
        private readonly SagaMongoDbContext _sagaMongoDbContext;
        protected SagaState _sagaState;
        protected readonly Dictionary<TTrigger, object> _triggerParams = new();
        protected readonly StateMachine<TState, TTrigger> _machine;
        public Guid CorrelationId { get { return _sagaState.CorrelationId; } }

        protected BaseSagaOrchestrator()
        {

        }

        protected BaseSagaOrchestrator(ISagaRepository sagaRepository, IOutboxRepository outboxRepository, ISagaUnitOfWork sagaUnitOfWork, ISagaStateHistoryRepository sagaStateHistoryRepository, SagaMongoDbContext sagaMongoDbContext)
        {
            this._sagaRepository = sagaRepository;
            this._outboxRepository = outboxRepository;
            this._sagaUnitOfWork = sagaUnitOfWork;
            this._sagaStateHistoryRepository = sagaStateHistoryRepository;
            this._sagaMongoDbContext = sagaMongoDbContext;
            _machine = new StateMachine<TState, TTrigger>(
                () => Enum.Parse<TState>(_sagaState?.CurrentState ?? default(TTrigger).ToString()),
                s => _sagaState.CurrentState = s.ToString()
            );

            //_machine.OnTransitionedAsync(async t =>
            //{

            //    string triggername = t.Trigger.ToString();
            //    _sagaState.TriggerName = triggername;
            //    _sagaState.Payload = t.Parameters != null && t.Parameters.Any() ? JsonConvert.SerializeObject(t.Parameters[0]) : "";

            //    if (!_sagaState.SaveStateInPublish)
            //    {
            //        await SaveStateAsync(t.Source.ToString());
            //    }
            //    else
            //    {

            //        //_sagaState = await _sagaRepository.FindAsync(_sagaState.Id);
            //        _sagaState.SaveStateInPublish = false;
            //        await _sagaUnitOfWork.SaveChangesAsync();
            //        //using SagaContext context = await _contextFactory.CreateDbContextAsync();
            //        //context.Set<SagaState>().Update(_sagaState);
            //        //await context.SaveChangesAsync();
            //    }


            //    if (IsFinalState(t.Destination))
            //    {
            //        _sagaState.TriggerName = "";
            //        _sagaState.Payload = "";
            //        _sagaState.SaveStateInPublish = false;
            //        _sagaState.Done = true;
            //        await SaveStateAsync(t.Destination.ToString());
            //        await LastStateAction();
            //    }
            //});

            ConfigureTriggerParameters(_machine);
            Configure(_machine);
        }

        protected abstract Task Configure(StateMachine<TState, TTrigger> machine);

        /// <summary>
        /// جایی که نیاز به پاس دادن پارامتر ورودی نیست از این متد استفاده می کنیم
        /// </summary>
        /// <param name="correlationId"></param>
        /// <param name="trigger">
        /// فقط مشخص میکنیم چه تریگری باید اجرا بشه
        /// </param>
        /// <returns></returns>
        public async Task RunAsync(Guid? correlationId, TTrigger trigger, object? param)
        {
            await InitState(correlationId, trigger, param);

            if (_machine.CanFire(trigger))
            {
                await _machine.FireAsync(trigger, param);
            }
        }

        public async Task RunAsync<TParam>(Guid? correlationId, StateMachine<TState, TTrigger>.TriggerWithParameters<TParam> triggerWithParameters, TParam? param)
        {

            await InitState(correlationId, triggerWithParameters.Trigger, param);

            if (_machine.CanFire(triggerWithParameters, param))
                await _machine.FireAsync(triggerWithParameters, param);
        }

        private async Task InitState(Guid? correlationId, TTrigger trigger, object param)
        {
            try
            {

                if (correlationId == Guid.Empty || correlationId == null)
                {
                    _sagaState = new SagaState
                    {
                        CorrelationId = Guid.NewGuid(),
                        CurrentState = default(TState).ToString(),
                        OrchestrationName = GetOrchestrationName(),
                        Payload = "",
                        TriggerName = ""
                    };
                    return;
                }

                //using SagaContext context = await _contextFactory.CreateDbContextAsync(); // ⬅️ ایجاد نمونه جدید
                _sagaState = await _sagaRepository.GetBySagaIdAsync(correlationId.Value);  //await context.Set<SagaState>().Include(s => s.SagaStateHistories).FirstOrDefaultAsync(r => r.CorrelationId == correlationId);

                if (_sagaState != null)
                {
                    if (_sagaState.SaveStateInPublish && _sagaState.TriggerName != trigger.ToString())
                    {
                        _sagaState.TriggerName = trigger.ToString();
                        _sagaState.Payload = param != null ? JsonConvert.SerializeObject(param) : "";


                        SagaStateHistory? h = _sagaState.SagaStateHistories.FirstOrDefault(s => s.State == _sagaState.CurrentState);

                        //await _sagaStateHistoryRepository.InsertAsync(h);

                        h.TriggerName = _sagaState.TriggerName;
                        h.Payload = _sagaState.Payload;

                        await _sagaUnitOfWork.SaveChangesAsync();

                    }
                }
                else
                {
                    _sagaState = new SagaState
                    {
                        CorrelationId = Guid.NewGuid(),
                        CurrentState = default(TState).ToString(),
                        OrchestrationName = GetOrchestrationName(),
                        Payload = "",
                        TriggerName = ""
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }

        }

        protected async Task SaveStateAsync(string state = "")
        {

            if (_sagaState == null) return;

            string machineState = _machine.State.ToString();
            if (!string.IsNullOrEmpty(state))
                _sagaState.CurrentState = state;

            SagaState? dbsaga = await _sagaRepository.GetById(_sagaState.Id);

            SagaStateHistory sagaStateHistory = null;

            try
            {

                bool hasState = dbsaga?.SagaStateHistories == null ? true : dbsaga.SagaStateHistories.Any(s => s.State == _sagaState.CurrentState);
                if (!hasState)
                {
                    sagaStateHistory = (new SagaStateHistory() { SagaId = _sagaState.Id, State = _sagaState.CurrentState, Payload = _sagaState.Payload, TriggerName = _sagaState.TriggerName });
                    dbsaga.SagaStateHistories.Add(sagaStateHistory);
                    //await _sagaStateHistoryRepository.InsertAsync(sagaStateHistory);

                    _sagaState.SaveStateInPublish = false;
                }


                if (dbsaga != null)
                {
                    UpdateModel(sagaStateHistory, dbsaga);
                }
                else
                {
                    _sagaState.SagaStateHistories.Add((new SagaStateHistory() { SagaId = _sagaState.Id, State = _sagaState.CurrentState, Payload = _sagaState.Payload, TriggerName = _sagaState.TriggerName }));
                    await _sagaRepository.InsertAsync(_sagaState);
                    //await dbContext.Set<SagaState>().AddAsync(_sagaState);
                }

                await _sagaUnitOfWork.SaveChangesAsync();

                ////اینجا دوباره وضعیت ماشین رو برمیگردونیم به حالت اصلیش
                _sagaState.CurrentState = machineState;

            }
            catch (Exception ex)
            {
                Exception a = ex;
            }


        }

        private void UpdateModel(SagaStateHistory? h, SagaState dbsaga)
        {
            dbsaga.SaveStateInPublish = _sagaState.SaveStateInPublish;
            dbsaga.Payload = _sagaState.Payload;
            dbsaga.TriggerName = _sagaState.TriggerName;
            dbsaga.CurrentState = _sagaState.CurrentState;
            dbsaga.Done = _sagaState.Done;
            dbsaga.Faile = _sagaState.Faile;

            if (h != null)
                dbsaga.SagaStateHistories.Add(h);
        }

        protected StateMachine<TState, TTrigger>.TriggerWithParameters<TParam> SetTriggerParameter<TParam>(TTrigger trigger)
        {
            StateMachine<TState, TTrigger>.TriggerWithParameters<TParam> triggerParam = _machine!.SetTriggerParameters<TParam>(trigger);
            _triggerParams[trigger] = triggerParam;
            return triggerParam;
        }
        protected async Task PublishAsync<T>(T message) where T : SagaBaseEvent
        {
            if (message is null)
                return;

            T msgWithId = message with { CorrelationId = CorrelationId }; // immutable record

            await _outboxRepository.AddAsync(new OutboxMessage(typeof(T).Name, JsonConvert.SerializeObject(msgWithId)));

            SagaState? saga = await _sagaRepository.GetById(_sagaState.Id);
            //await context.Set<SagaState>().Include(a => a.SagaStateHistories).FirstOrDefaultAsync(s => s.Id == _sagaState.Id);
            if (saga != null)
            {
                saga.CurrentState = _machine.State.ToString();
                saga.Payload = "";
                saga.TriggerName = "";
                saga.SaveStateInPublish = true;

                SagaStateHistory history = (new SagaStateHistory() { SagaId = saga.Id, State = _machine.State.ToString(), Payload = _sagaState.Payload, TriggerName = _sagaState.TriggerName });

                saga.SagaStateHistories.Add(history);

                _sagaMongoDbContext.Entry(history).State = Microsoft.EntityFrameworkCore.EntityState.Added;


                await _sagaUnitOfWork.SaveChangesAsync();
            }
            else
            {
                Console.WriteLine($"saga is null in publish {_sagaState}");
                //await context.Set<SagaState>().AddAsync(_sagaState);
            }
        }

        public abstract Type GetStateType();
        public abstract Type GetTriggerType();
        public abstract string GetOrchestrationName();
        public abstract Type GetOrchestrationType();
        protected abstract bool IsFinalState(TState state);
        protected abstract Task ConfigureTriggerParameters(StateMachine<TState, TTrigger> machine);
        public abstract Type? GetPayloadType(Enum orderCreateTrigger);

        public virtual async Task LastStateAction()
        {
            Console.WriteLine($" {GetOrchestrationName()} Done");
        }
    }
}

-----------------------


using Saga.Core.Contracts;
using Saga.Core.Orchestrators.Order.OrderCreateOrchestration;
using Saga.Infrastructure.Persistence.Context;
using SCore.Framework.BuildingBlocks.OutboxInbox;
using SCore.Shared.Event.OrderCrateOrchestrator;
using Stateless;

namespace Saga.Endpoint.Orchestrators.Order.OrderCreateOrchestration
{
    public interface IOrderCrateOrchestrator : ISaga<OrderCreateState, OrderCreateTrigger>
    {
        StateMachine<OrderCreateState, OrderCreateTrigger>.TriggerWithParameters<PaymentProcess> PaymentProcessConfirmTrigger { set; get; }
        StateMachine<OrderCreateState, OrderCreateTrigger>.TriggerWithParameters<inventoryProcess> InventoryProcessConfirmTrigger { set; get; }
    }

    public class OrderCrateOrchestrator : BaseSagaOrchestrator<OrderCreateState, OrderCreateTrigger>, IOrderCrateOrchestrator
    {
        private readonly ILogger<OrderCrateOrchestrator> _logger;

        public StateMachine<OrderCreateState, OrderCreateTrigger>.TriggerWithParameters<PaymentProcess> PaymentProcessConfirmTrigger { set; get; }
        public StateMachine<OrderCreateState, OrderCreateTrigger>.TriggerWithParameters<inventoryProcess> InventoryProcessConfirmTrigger { set; get; }


        public OrderCrateOrchestrator() : base()
        {

        }

        public OrderCrateOrchestrator(ISagaRepository sagaRepository, IOutboxRepository outboxRepository, ISagaUnitOfWork sagaUnitOfWork, ISagaStateHistoryRepository sagaStateHistoryRepository, ILogger<OrderCrateOrchestrator> logger ,  SagaMongoDbContext sagaMongoDbContext) : base(sagaRepository, outboxRepository, sagaUnitOfWork, sagaStateHistoryRepository, sagaMongoDbContext)
        {
            _logger = logger;
        }

        protected override bool IsFinalState(OrderCreateState state) => state == OrderCreateState.Done || state == OrderCreateState.Fail;

        protected virtual object? PreparePayloadForTrigger(OrderCreateTrigger trigger)
        {
            return null;
        }
        protected override async Task Configure(StateMachine<OrderCreateState, OrderCreateTrigger> machine)
        {

            _machine.Configure(OrderCreateState.Initial)
                  .OnExitAsync(async () =>
                  {
                      _logger.LogInformation("OnExitAsync Initial");
                      await SaveStateAsync(OrderCreateState.Initial.ToString());
                  })
                 .Permit(OrderCreateTrigger.OrderCreateStarted, OrderCreateState.OrderCreateStarted);

            _machine.Configure(OrderCreateState.OrderCreateStarted)
                  //.OnEntryAsync(async () => { _logger.LogInformation("OnEntryAsync OrderCreateStarted"); })
                  .OnExitAsync(async () => { _logger.LogInformation("OnExitAsync OrderCreateStarted"); })
                  .OnActivateAsync(async () => { _logger.LogInformation("OnActivateAsync OrderCreateStarted"); })
                  .OnEntryFromAsync(PaymentProcessConfirmTrigger, async (payment) =>
                  {
                      _logger.LogInformation("OnEntryAsync OrderCreateStarted");
                      //await _machine.FireAsync(OrderCreateTrigger.PaymentSucceded);

                      await PublishAsync(payment);
                  })
            .Permit(OrderCreateTrigger.PaymentSucceded, OrderCreateState.PaymentSucceded)
            .Permit(OrderCreateTrigger.PaymentFailed, OrderCreateState.PaymentFailed);


            _machine.Configure(OrderCreateState.PaymentSucceded)
                  .OnEntryFromAsync(InventoryProcessConfirmTrigger, async (inventory) =>
                  {
                      await _machine.FireAsync(OrderCreateTrigger.InventorySucceded);
                      //await PublishAsync(inventory);
                  })
            .Permit(OrderCreateTrigger.InventorySucceded, OrderCreateState.InventorySucceded)
            .Permit(OrderCreateTrigger.InventoryFailed, OrderCreateState.InventoryFailed);

            _machine.Configure(OrderCreateState.PaymentFailed)
                .OnEntryAsync(async () =>
                {
                    /// todo  publish
                });

            _machine.Configure(OrderCreateState.InventoryFailed)
                .OnEntryAsync(async () =>
                {
                    /// todo  publish
                });

            _machine.Configure(OrderCreateState.InventorySucceded)
                .OnEntryAsync(async () =>
                {
                    try
                    {
                        await _machine.FireAsync(OrderCreateTrigger.Complete);

                    }
                    catch (Exception)
                    {

                        throw;
                    }
                })
                .Permit(OrderCreateTrigger.Complete, OrderCreateState.Done);
            return;
        }

        protected override async Task ConfigureTriggerParameters(StateMachine<OrderCreateState, OrderCreateTrigger> machine)
        {
            PaymentProcessConfirmTrigger = _machine.SetTriggerParameters<PaymentProcess>(OrderCreateTrigger.OrderCreateStarted);
            InventoryProcessConfirmTrigger = _machine.SetTriggerParameters<inventoryProcess>(OrderCreateTrigger.PaymentSucceded);
            return;
        }

        public override string GetOrchestrationName()
        {
            return nameof(OrderCrateOrchestrator);
        }

        public override Type GetOrchestrationType()
        {
            return typeof(IOrderCrateOrchestrator);
        }

        public override Type GetStateType()
        {
            return typeof(OrderCreateState);
        }

        public override Type GetTriggerType()
        {
            return typeof(OrderCreateTrigger);
        }

        public override Type? GetPayloadType(Enum orderCreateTrigger)
        {
            return orderCreateTrigger switch
            {
                OrderCreateTrigger.OrderCreateStarted => typeof(PaymentProcess),
                OrderCreateTrigger.PaymentSucceded => typeof(inventoryProcess),
                OrderCreateTrigger.PaymentFailed => typeof(inventoryProcess),
                _ => null
            };
        }

    }
}
-----------------------
using Microsoft.EntityFrameworkCore;
using Saga.Core.Contracts;
using Saga.Core.Entites;
using Saga.Infrastructure.Persistence.Context;
using SCore.Framework.Infrastructure.Persistence.Repositories;

namespace Saga.Infrastructure.Repositories
{

    public class SagaStatesRepository : BaseRepository<SagaState, Guid, SagaMongoDbContext>, ISagaRepository
    {
        public SagaStatesRepository(SagaMongoDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<SagaState> GetBySagaIdAsync(Guid? SagaID)
        {
            return await _dbSet.FirstOrDefaultAsync(a => a.CorrelationId == SagaID);
        }


        public async Task<SagaState> GetById(Guid? id)
        {
            return await _dbSet.FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<IEnumerable<SagaState>> GetUnProceess()
        {
            return await _dbSet.AsNoTracking().Where(s => !s.Done && !s.Faile && !string.IsNullOrEmpty(s.TriggerName)).ToListAsync();
        }
    }


    public class SagaStateHistoryRepository : BaseRepository<SagaStateHistory, Guid, SagaContext>, ISagaStateHistoryRepository
    {
        public SagaStateHistoryRepository(SagaContext dbContext) : base(dbContext)
        {
        }


        public async Task<SagaStateHistory> GetBySagaId(Guid id)
        {
            return await _dbSet.FirstOrDefaultAsync(s => s.SagaId == id);

        }

    }
}
------------------
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using SCore.Framework.BuildingBlocks.Mediator.Event;
using SCore.Framework.BuildingBlocks.OutboxInbox;
using SCore.Framework.Infrastructure.Persistence.Context;

namespace SCore.Framework.Infrastructure.Persistence.Repositories
{
    public class BaseOutBoxRepository<TDbContext> : IOutboxRepository
        where TDbContext : BaseApplicationContext
    {
        private readonly TDbContext _dbContext;
        private readonly DbSet<OutboxMessage> _outbox;
        public BaseOutBoxRepository(TDbContext dbContext)
        {

            this._dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _outbox = _dbContext.Set<OutboxMessage>();
        }

        public async Task<OutboxMessage> FindAsync(long code)
        {
            return await _outbox.FindAsync(code);
        }

        public OutboxMessage Find(long code)
        {
            return _outbox.Find(code);
        }

        public async Task<OutboxMessage> FirstOrDefaultAsync(Guid id)
        {
            return await _outbox.FirstOrDefaultAsync(row => row.CorrelationId == id);
        }

        public OutboxMessage FirstOrDefault(Guid id)
        {
            return _outbox.FirstOrDefault(row => row.CorrelationId == id);
        }


        public void Add<T>(T eventObject) where T : IEvent
        {
            string? eventType = eventObject.GetType().FullName;
            string eventJson = JsonConvert.SerializeObject(eventObject);

            Add(new(eventType, eventJson));
        }

        public async Task AddAsync<T>(T eventObject) where T : IEvent
        {
            string? eventType = eventObject.GetType().FullName;
            string eventJson = JsonConvert.SerializeObject(eventObject);

            await AddAsync(new(eventType, eventJson));
        }


        public void Add(OutboxMessage message)
        {
            _outbox.Add(message);
        }

        public async Task AddAsync(OutboxMessage message)
        {
            await _outbox.AddAsync(message);
        }

        public void AddRange(List<OutboxMessage> messages)
        {
            _outbox.AddRange(messages);
        }

        public async Task AddRangeAsync(List<OutboxMessage> messages)
        {
            await _outbox.AddRangeAsync(messages);
        }

        public async Task<List<OutboxMessage>> GetPendingAsync()
        {
            return await _outbox.Where(row => row.Status == OutboxStatus.Pending).ToListAsync();
        }

        public async Task<List<OutboxMessage>> GetPendingAsync(int takeCount)
        {
            return await _outbox
                .Where(row => row.Status == OutboxStatus.Pending || (row.Status == OutboxStatus.InProgress /*&& row.ProcessedDate.HasValue && DateTime.Now > row.ProcessedDate.Value.AddMinutes(2)*/ ))
                .OrderBy(s => s.Id)
                .ThenBy(s => s.Priority)
                .ThenBy(s => s.RetryCount)
                .Take(takeCount)
                .ToListAsync();
        }

        public int Save()
        {
            return _dbContext.SaveChanges();
        }
        public async Task<int> SaveAsync(CancellationToken cancellationToken = default)
        {
            return await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task<int> UpdateStatusInProgress(List<long> outboxes)
        {
            return await _outbox.Where(s => outboxes.Contains(s.Id))
               .ExecuteUpdateAsync(s =>
               s.SetProperty(m => m.Status, m => OutboxStatus.InProgress)
               .SetProperty(m => m.ProcessedDate, m => DateTime.Now)
               .SetProperty(m => m.RetryCount, m => m.RetryCount + 1)
               );
        }

        public async Task<int> UpdateStatus(List<long> ids, bool failed, string errorMessage = "")
        {
            return await _outbox.Where(s => ids.Contains(s.Id))
                .ExecuteUpdateAsync(s =>
                s.SetProperty(m => m.Status, m => !failed ? m.RetryCount > 5 ? OutboxStatus.Failed : OutboxStatus.Pending : OutboxStatus.Processed)
                .SetProperty(m => m.ProcessedDate, m => DateTime.Now)
                .SetProperty(m => m.Priority, m => m.RetryCount > 5 ? OutboxPriority.Low : m.RetryCount > 3 ? OutboxPriority.Normal : m.Priority)
                .SetProperty(m => m.LastErrorMessage, m => errorMessage)
                );
        }


        public async Task<int> UpdateErrorMessage(long id, string errorMessage)
        {
            return await _outbox.Where(s => s.Id == id)
                .ExecuteUpdateAsync(s =>
                s.SetProperty(m => m.LastErrorMessage, m => errorMessage)
                );
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;
using SCore.Framework.Application.Contracts;
using SCore.Framework.BuildingBlocks.Shared.DateTimeProvider;
using SCore.Framework.Domain.Entites;
using SCore.Framework.Infrastructure.EventDispatching;
using SCore.Framework.Infrastructure.Exceptions;

namespace SCore.Framework.Infrastructure.Persistence.Context
{

    public class BaseUnitOfWork<TdbContext> : IBaseUnitOfWork
         where TdbContext : BaseApplicationContext
    {
        private readonly TdbContext _db;
        private readonly IDomainEventsDispatcher<TdbContext> _domainEventsDispatcher;
        private IDbContextTransaction? _transaction;


        public void Dispose()
        {
            _transaction?.Dispose();
        }

        public BaseUnitOfWork(TdbContext dbContext, IDomainEventsDispatcher<TdbContext> domainEventsDispatcher)
        {
            if (dbContext is BaseQueryContext)
                throw new InValidQueryContextForSaveChangesExeption();

            _db = dbContext;
            _domainEventsDispatcher = domainEventsDispatcher;
        }

        public virtual void BeginTransaction()
        {
            _db.Database.BeginTransaction();
        }

        public virtual async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_transaction == null)
                _transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        }

        public virtual async Task<int> SaveChanges()
        {
            await BeforSave();
            return _db.SaveChanges();
        }

        public virtual async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await BeforSave();

            var asdad = _db.ChangeTracker.Entries();

            return await _db.SaveChangesAsync(cancellationToken);
        }

        public virtual void CommitTransaction()
        {
            _db.Database.CommitTransaction();
        }

        public virtual async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_transaction != null)
            {
                await _db.SaveChangesAsync(cancellationToken);
                await _transaction.CommitAsync(cancellationToken);
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }


        public virtual void RollbackTransaction()
        {
            _db.Database.RollbackTransaction();
        }

        public virtual async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync(cancellationToken);
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        private async Task BeforSave()
        {
            SetAudit();
            await _domainEventsDispatcher.DispatchDomainEventsAsync();
        }

        private void SetAudit()
        {
            List<EntityEntry<IAuditable>> Entries = _db.ChangeTracker.Entries<IAuditable>().Where(s => s.State == EntityState.Deleted || s.State == EntityState.Modified || s.State == EntityState.Added).ToList();
            foreach (EntityEntry<IAuditable> entity in Entries)
            {
                switch (entity.State)
                {
                    case EntityState.Deleted:
                        throw new SoftDeleteExeption("you can not hard deleted entity, use ISoftDelete interface for entity ");

                    case EntityState.Modified:
                        (entity.Entity).SetAuditLastModifier("", DateTimeProvider.Now(), "");
                        break;

                    case EntityState.Added:
                        (entity.Entity).SetAuditCreator("-", DateTimeProvider.Now(), "-");
                        break;

                    default:
                        break;
                }
            }
        }
    }
}

