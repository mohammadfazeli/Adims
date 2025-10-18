using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

public class EfUnitOfWork : IUnitOfWork
{
    private readonly YourDbContext _db;
    private IDbContextTransaction? _transaction;

    public EfUnitOfWork(YourDbContext db)
    {
        _db = db;
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction == null)
            _transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            await _db.SaveChangesAsync(cancellationToken);
            await _transaction.CommitAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _db.SaveChangesAsync(cancellationToken);

    public void Dispose()
    {
        _transaction?.Dispose();
    }
}


using MediatR;

public class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public TransactionBehavior(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        await _unitOfWork.BeginTransactionAsync(ct);

        try
        {
            var response = await next();
            await _unitOfWork.CommitAsync(ct);
            return response;
        }
        catch
        {
            await _unitOfWork.RollbackAsync(ct);
            throw;
        }
    }
}



public interface IInboxService
{
    Task<bool> AlreadyProcessedAsync(string messageId);
    Task AddAsync(string messageId);
    Task MarkProcessedAsync(string messageId);
}


using Microsoft.EntityFrameworkCore;

public class InboxService : IInboxService
{
    private readonly YourDbContext _db;

    public InboxService(YourDbContext db)
    {
        _db = db;
    }

    public Task<bool> AlreadyProcessedAsync(string messageId)
        => _db.InboxMessages.AnyAsync(x => x.MessageId == messageId && x.Processed);

    public async Task AddAsync(string messageId)
    {
        var exists = await _db.InboxMessages.AnyAsync(x => x.MessageId == messageId);
        if (!exists)
        {
            _db.InboxMessages.Add(new InboxMessage
            {
                MessageId = messageId,
                ReceivedAt = DateTime.UtcNow,
                Processed = false
            });
            await _db.SaveChangesAsync();
        }
    }

    public async Task MarkProcessedAsync(string messageId)
    {
        var msg = await _db.InboxMessages.FirstOrDefaultAsync(x => x.MessageId == messageId);
        if (msg != null)
        {
            msg.Processed = true;
            msg.ProcessedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }
}


public interface ISagaStateRepository
{
    Task<SagaState> GetAsync(Guid orderId);
    Task UpdateAsync(SagaState state);
}

public class SagaStateRepository : ISagaStateRepository
{
    private readonly YourDbContext _db;

    public SagaStateRepository(YourDbContext db)
    {
        _db = db;
    }

    public Task<SagaState> GetAsync(Guid orderId)
        => _db.SagaStates.FindAsync(orderId).AsTask();

    public Task UpdateAsync(SagaState state)
    {
        _db.SagaStates.Update(state);
        return _db.SaveChangesAsync();
    }
}

-------‐------
using MediatR;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<YourDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// UnitOfWork
builder.Services.AddScoped<IUnitOfWork, EfUnitOfWork>();

// MediatR
builder.Services.AddMediatR(typeof(Program));

// Transaction Behavior
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));

// Inbox
builder.Services.AddScoped<IInboxService, InboxService>();

// Saga Repository
builder.Services.AddScoped<ISagaStateRepository, SagaStateRepository>();

// Rabbit Consumer
builder.Services.AddScoped<RabbitConsumer>();

var app = builder.Build();
app.Run();



----------


public async Task OnRabbitMessageReceived(string messageId, Guid orderId)
{
    using var scope = serviceProvider.CreateScope();
    var consumer = scope.ServiceProvider.GetRequiredService<RabbitConsumer>();

    await consumer.HandleMessageAsync(messageId, new StartOrderCommand(orderId));
}



