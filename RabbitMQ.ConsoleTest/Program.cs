using Microsoft.Extensions.Logging.Abstractions;
using RabbitMQ;

using var mqClient = new RabbitMqClient(NullLogger<RabbitMqClient>.Instance, "localhost");

await mqClient.CreateReceiving<long>("queue-1", (value) =>
{
    Console.WriteLine($"queue-1: {value}");
    mqClient.SendMessageAsync("queue-2", value);
},CancellationToken.None);

await mqClient.CreateReceiving<long>("queue-2", (value) =>
{
    Console.WriteLine($"queue-2: {value}");
    mqClient.SendMessageAsync("queue-3", value);
},CancellationToken.None);

await mqClient.CreateReceiving<long>("queue-3", (value) =>
{
    Console.WriteLine($"queue-3: {value}");
},CancellationToken.None);


await mqClient.SendMessageAsync("queue-1", DateTime.Now.Ticks);
Console.ReadKey();