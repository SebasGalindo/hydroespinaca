using SensorService.Domain.Interfaces;

namespace SensorService.Application.UseCases.ProcessReadingBatch;
public class MqttMessageDispatcher
{
    private readonly Dictionary<string, IMqttMessageHandler> _handlers;

    public MqttMessageDispatcher(IEnumerable<IMqttMessageHandler> handlers)
    {
        _handlers = new Dictionary<string, IMqttMessageHandler>
        {
            { "sensor/", handlers.OfType<MqttMessageHandler>().First() }
        };
    }

    public async Task DispatchAsync(string topic, byte[] payload, CancellationToken cancellationToken)
    {
        foreach (var (prefix, handler) in _handlers)
        {
            if (topic.StartsWith(prefix))
            {
                await handler.HandleAsync(topic, payload, cancellationToken);
                return;
            }
        }

        // No handler found
        Console.WriteLine($"⚠️ No handler para topic: {topic}");
    }
}
