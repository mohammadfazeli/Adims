using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

public class RabbitMqSubscriber : IMessageSubscriber, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;

    public RabbitMqSubscriber(string hostName = "localhost")
    {
        var factory = new ConnectionFactory() { HostName = hostName };
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
    }

    public Task SubscribeAsync<TMessage>(
        string queueName,
        Func<TMessage, Task> handler,
        CancellationToken cancellationToken = default)
    {
        _channel.QueueDeclare(queue: queueName,
                             durable: true,
                             exclusive: false,
                             autoDelete: false,
                             arguments: null);

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.Received += async (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var json = Encoding.UTF8.GetString(body);
                var message = JsonSerializer.Deserialize<TMessage>(json);

                if (message != null)
                {
                    await handler(message);
                    _channel.BasicAck(ea.DeliveryTag, multiple: false);
                }
                else
                {
                    _channel.BasicNack(ea.DeliveryTag, false, false);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RabbitMqSubscriber] Error: {ex.Message}");
                _channel.BasicNack(ea.DeliveryTag, false, true);
            }
        };

        _channel.BasicConsume(queue: queueName,
                             autoAck: false,
                             consumer: consumer);

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
}
