// وقتی سفارش ساخته شد
public class OrderCreatedPayload
{
    public Guid OrderId { get; set; }
    public Guid CustomerId { get; set; }
    public decimal TotalAmount { get; set; }
}

// اگر یکی از مراحل Saga شکست خورد
public class FailurePayload
{
    public Guid CorrelationId { get; set; }   // برای ارتباط بین Stepهای مختلف
    public string Reason { get; set; }
    public DateTime FailedAt { get; set; } = DateTime.UtcNow;
}


public interface ISagaRepository<TSagaState>
{
    Task SaveAsync(TSagaState state);
    Task<TSagaState?> GetAsync(Guid correlationId);
    Task UpdateAsync(TSagaState state);
    Task DeleteAsync(Guid correlationId);
}



public class InMemorySagaRepository<TSagaState> : ISagaRepository<TSagaState>
    where TSagaState : class
{
    private readonly Dictionary<Guid, TSagaState> _storage = new();

    public Task SaveAsync(TSagaState state)
    {
        var correlationId = (Guid)typeof(TSagaState).GetProperty("CorrelationId")!.GetValue(state)!;
        _storage[correlationId] = state;
        return Task.CompletedTask;
    }

    public Task<TSagaState?> GetAsync(Guid correlationId)
    {
        _storage.TryGetValue(correlationId, out var state);
        return Task.FromResult(state);
    }

    public Task UpdateAsync(TSagaState state)
    {
        var correlationId = (Guid)typeof(TSagaState).GetProperty("CorrelationId")!.GetValue(state)!;
        _storage[correlationId] = state;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid correlationId)
    {
        _storage.Remove(correlationId);
        return Task.CompletedTask;
    }
}

public enum OrderSagaStateEnum
{
    Started,
    PaymentPending,
    PaymentCompleted,
    InventoryReserved,
    Completed,
    Failed
}

public enum OrderSagaTrigger
{
    Start,
    PaymentSucceeded,
    PaymentFailed,
    InventoryReserved,
    InventoryFailed,
    Complete
}

public class OrderSagaState
{
    public Guid CorrelationId { get; set; } = Guid.NewGuid();
    public OrderSagaStateEnum State { get; set; } = OrderSagaStateEnum.Started;

    public Guid OrderId { get; set; }
    public Guid CustomerId { get; set; }
    public decimal TotalAmount { get; set; }
}
