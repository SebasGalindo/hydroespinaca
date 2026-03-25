using Microsoft.Extensions.Logging;
using SensorService.Domain.Interfaces;

namespace SensorService.Application.UseCases.ProcessReadingBatch;

/// <summary>
/// Despachador de mensajes MQTT que enruta los mensajes entrantes al handler correspondiente
/// según el tópico. Mantiene un diccionario de handlers registrados por tópico MQTT.
/// </summary>
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

    /// <summary>
    /// Despacha un mensaje MQTT al handler registrado para el tópico especificado.
    /// Si no existe un handler para el tópico, registra una advertencia.
    /// </summary>
    /// <param name="topic">Tópico MQTT del mensaje entrante.</param>
    /// <param name="payload">Contenido del mensaje en bytes.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
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
