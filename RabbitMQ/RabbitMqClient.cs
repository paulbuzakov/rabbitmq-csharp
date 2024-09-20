using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RabbitMQ;

public class RabbitMqClient : IRabbitMqClient
{
    private readonly ILogger<RabbitMqClient> _logger;
    private IConnection _connection;
    private IModel _channel;

    public RabbitMqClient(ILogger<RabbitMqClient> logger, IConfiguration configuration)
    {
        _logger = logger;

        var hostname = configuration.GetValue<string>("RabbitMQ:HostName")
                       ?? throw new ArgumentException("RabbitMQ:HostName");

        Initialize(hostname);
    }

    public RabbitMqClient(ILogger<RabbitMqClient> logger, string hostname)
    {
        _logger = logger;

        Initialize(hostname);
    }

    private void Initialize(string hostname)
    {
        var factory = new ConnectionFactory { HostName = hostname };
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
    }

    public Task CreateReceiving<T>(string queue, Action<T> onReceived, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        _channel.QueueDeclare(queue: queue, durable: false, exclusive: false, autoDelete: false, arguments: null);
        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += (ch, ea) =>
        {
            var obj = JsonSerializer.Deserialize<T>(ea.Body.ToArray());

            if (obj != null)
            {
                onReceived.Invoke(obj);
            }

            _channel.BasicAck(ea.DeliveryTag, false);
        };

        _channel.BasicConsume(queue, false, consumer);

        return Task.CompletedTask;
    }

    public Task SendMessageAsync(string queue, string message, CancellationToken cancellationToken = default)
    {
        _channel.QueueDeclare(
            queue: queue,
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        var body = Encoding.UTF8.GetBytes(message);

        _channel.BasicPublish(
            exchange: "",
            routingKey: queue,
            basicProperties: null,
            body: body);

        return Task.CompletedTask;
    }

    public Task SendMessageAsync(string queue, object obj, CancellationToken cancellationToken = default)
    {
        var message = JsonSerializer.Serialize(obj);
        SendMessageAsync(queue, message, cancellationToken);

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _channel.Dispose();
        _connection.Dispose();
    }
}