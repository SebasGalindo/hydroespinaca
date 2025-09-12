using SensorService.Domain.Interfaces;

namespace SensorService.Application.UseCases.ProcessReadingBatch;
public class MqttMessageDispatcher
{
    private readonly Dictionary<string, IMqttMessageHandler> _handlers;

    public MqttMessageDispatcher(IEnumerable<IMqttMessageHandler> handlers)
    {
        _handlers = new Dictionary<string, IMqttMessageHandler>
        {
            { "sensor/readings", handlers.OfType<MqttMessageHandler>().First() }
        };
    }

    public async Task DispatchAsync(string topic, byte[] payload, CancellationToken cancellationToken)
    {
        if (_handlers.TryGetValue(topic, out var handler))
        {
            await handler.HandleAsync(topic, payload, cancellationToken);
            return;
        }

        // No handler found for this topic
        Console.WriteLine($"⚠️ No handler para topic: {topic}");
    }
}
