using Microsoft.Extensions.Logging;
using SensorService.Domain.Interfaces;

namespace SensorService.Application.UseCases.ProcessReadingBatch;
public class MqttMessageDispatcher
{
    private readonly Dictionary<string, IMqttMessageHandler> _handlers;
    private readonly ILogger<MqttMessageDispatcher> _logger;

    public MqttMessageDispatcher(
        IEnumerable<IMqttMessageHandler> handlers,
        ILogger<MqttMessageDispatcher> logger)
    {
        _logger = logger;
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

        _logger.LogWarning("No se encontró handler para topic: {Topic}", topic);
    }
}
