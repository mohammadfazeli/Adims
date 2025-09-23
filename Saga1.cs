using Stateless;
using System.Text.Json;

// DTO برای نگهداری state و داده‌ی Saga در DB
public class SagaEntity
{
    public string SagaId { get; set; } = default!;
    public OrderState State { get; set; }
    public string DataJson { get; set; } = "{}";
    public string? LastProcessedMessageId { get; set; }
    public int Version { get; set; } // برای optimistic concurrency
}

// مدل دادهٔ داخلی
public class OrderSagaData
{
    public string OrderId { get; set; } = default!;
    public decimal Amount { get; set; }
    public int PaymentAttempts { get; set; }
    public string? LastError { get; set; }
    // ... هر فیلدی که لازم داری
}

// ساده‌سازی: بروکر انتزاعی
public interface IMessageBroker
{
    Task PublishAsync(string topic, string message, IDictionary<string,string>? headers = null);
    Task SubscribeAsync(string topic, Func<BrokerMessage, Task> handler);
}

public class BrokerMessage
{
    public string MessageId { get; set; } = Guid.NewGuid().ToString();
    public string SagaId { get; set; } = default!;
    public string Type { get; set; } = default!;
    public string PayloadJson { get; set; } = "{}";
    public IDictionary<string,string>? Headers { get; set; }
}
