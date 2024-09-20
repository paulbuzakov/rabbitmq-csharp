namespace RabbitMQ;

public interface IRabbitMqClient : IDisposable
{
    Task CreateReceiving<T>(string queue, Action<T> onReceived, CancellationToken cancellationToken = default);
    Task SendMessageAsync(string queue, string message, CancellationToken cancellationToken = default);
    Task SendMessageAsync(string queue, object message, CancellationToken cancellationToken = default);
}