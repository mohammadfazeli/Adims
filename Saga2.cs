public class OrderSagaOrchestrator
{
    private readonly IMessageBroker _broker;
    private readonly ISagaRepository _repo; // abstraction for persistence (DB)
    // concurrency considerations: may use distributed lock provider here

    public OrderSagaOrchestrator(IMessageBroker broker, ISagaRepository repo)
    {
        _broker = broker;
        _repo = repo;
    }

    public async Task StartAsync()
    {
        // subscribe to all relevant topics
        await _broker.SubscribeAsync("OrderCreated", HandleBrokerMessage);
        await _broker.SubscribeAsync("PaymentSucceeded", HandleBrokerMessage);
        await _broker.SubscribeAsync("PaymentFailed", HandleBrokerMessage);
        await _broker.SubscribeAsync("InventoryReserved", HandleBrokerMessage);
        await _broker.SubscribeAsync("InventoryFailed", HandleBrokerMessage);
        await _broker.SubscribeAsync("ShippingSucceeded", HandleBrokerMessage);
        await _broker.SubscribeAsync("ShippingFailed", HandleBrokerMessage);
        // ... و غیره
    }

    private async Task HandleBrokerMessage(BrokerMessage msg)
    {
        // 1. بارگذاری یا ایجاد Saga
        var saga = await _repo.LoadSagaAsync(msg.SagaId);
        if (saga == null)
        {
            saga = new SagaEntity
            {
                SagaId = msg.SagaId,
                State = OrderState.None,
                DataJson = "{}",
                Version = 0
            };
        }

        // Idempotency: اگر این پیام قبلاً پردازش شده، رد کن
        if (msg.MessageId == saga.LastProcessedMessageId)
        {
            return;
        }

        // تبدیل داده
        var data = JsonSerializer.Deserialize<OrderSagaData>(saga.DataJson) ?? new OrderSagaData();

        // create state machine with external state
        var stateMachine = new StateMachine<OrderState, OrderTrigger>(
            () => saga.State,
            s => saga.State = s
        );

        ConfigureStateMachine(stateMachine, saga, data);

        // handle message -> map to trigger
        try
        {
            // locking: نمونه ساده با optimistic concurrency: حین save امکان تشخیص collision هست
            // Fire the trigger based on message.Type
            switch (msg.Type)
            {
                case "OrderCreated":
                    // populate data from payload
                    var created = JsonSerializer.Deserialize<OrderCreatedPayload>(msg.PayloadJson);
                    data.OrderId = created.OrderId;
                    data.Amount = created.Amount;
                    // persist initial data
                    saga.DataJson = JsonSerializer.Serialize(data);
                    await _repo.SaveSagaAsync(saga); // initial save
                    await stateMachine.FireAsync(OrderTrigger.StartOrder);
                    break;
                case "PaymentSucceeded":
                    await stateMachine.FireAsync(OrderTrigger.PaymentSucceeded);
                    break;
                case "PaymentFailed":
                    // payload might contain reason
                    var pFail = JsonSerializer.Deserialize<FailurePayload>(msg.PayloadJson);
                    data.LastError = pFail?.Reason;
                    saga.DataJson = JsonSerializer.Serialize(data);
                    await _repo.SaveSagaAsync(saga);
                    await stateMachine.FireAsync(OrderTrigger.PaymentFailed);
                    break;
                case "InventoryReserved":
                    await stateMachine.FireAsync(OrderTrigger.InventoryReserved);
                    break;
                case "InventoryFailed":
                    await stateMachine.FireAsync(OrderTrigger.InventoryFailed);
                    break;
                case "ShippingSucceeded":
                    await stateMachine.FireAsync(OrderTrigger.ShippingSucceeded);
                    break;
                case "ShippingFailed":
                    await stateMachine.FireAsync(OrderTrigger.ShippingFailed);
                    break;
                case "CompensationCompleted":
                    await stateMachine.FireAsync(OrderTrigger.CompensationCompleted);
                    break;
                default:
                    // ignore or log
                    break;
            }

            // update last processed message id for idempotency
            saga.LastProcessedMessageId = msg.MessageId;
            saga.DataJson = JsonSerializer.Serialize(data);

            // Save saga (with optimistic concurrency handling)
            await _repo.SaveSagaAsync(saga);
        }
        catch (Exception ex)
        {
            // log, maybe schedule retry or mark saga failed
            data.LastError = ex.Message;
            saga.DataJson = JsonSerializer.Serialize(data);
            await _repo.SaveSagaAsync(saga);
            // optionally publish an event (SagaFailed)
        }
    }

    private void ConfigureStateMachine(StateMachine<OrderState, OrderTrigger> sm, SagaEntity saga, OrderSagaData data)
    {
        // Basic configuration
        sm.Configure(OrderState.None)
          .Permit(OrderTrigger.StartOrder, OrderState.PaymentPending);

        sm.Configure(OrderState.PaymentPending)
          .OnEntryAsync(async () =>
          {
              // publish command to charge payment
              var cmd = new { OrderId = data.OrderId, Amount = data.Amount };
              await _broker.PublishAsync("ChargePayment", JsonSerializer.Serialize(cmd), new Dictionary<string,string>{{"SagaId", saga.SagaId}});
          })
          .Permit(OrderTrigger.PaymentSucceeded, OrderState.PaymentCompleted)
          .Permit(OrderTrigger.PaymentFailed, OrderState.Compensating);

        sm.Configure(OrderState.PaymentCompleted)
          .Permit(OrderTrigger.InventoryReserved, OrderState.InventoryReserved)
          .Permit(OrderTrigger.InventoryFailed, OrderState.Compensating);

        sm.Configure(OrderState.InventoryReserved)
          .OnEntryAsync(async () =>
          {
              // send shipping request
              var cmd = new { OrderId = data.OrderId };
              await _broker.PublishAsync("RequestShipping", JsonSerializer.Serialize(cmd), new Dictionary<string,string>{{"SagaId", saga.SagaId}});
          })
          .Permit(OrderTrigger.ShippingSucceeded, OrderState.Shipped)
          .Permit(OrderTrigger.ShippingFailed, OrderState.Compensating);

        sm.Configure(OrderState.Shipped)
          .OnEntry(() =>
          {
              // final step
          })
          .Permit(OrderTrigger.ShippingSucceeded, OrderState.Completed);

        sm.Configure(OrderState.Compensating)
          .OnEntryAsync(async () =>
          {
              // run compensation actions based on where we are
              // e.g., if payment was taken, publish RefundPayment
              if (saga.State == OrderState.PaymentCompleted)
              {
                  var refundCmd = new { OrderId = data.OrderId, Amount = data.Amount };
                  await _broker.PublishAsync("RefundPayment", JsonSerializer.Serialize(refundCmd), new Dictionary<string,string>{{"SagaId", saga.SagaId}});
              }

              // when compensation is done, publish CompensationCompleted
          })
          .Permit(OrderTrigger.CompensationCompleted, OrderState.Failed);

        // Transition events for side-effects
        sm.OnTransitioned(t =>
        {
            // logging or telemetry
            Console.WriteLine($"Saga {saga.SagaId}: {t.Source} -> {t.Destination} via {t.Trigger}");
        });
    }
}
