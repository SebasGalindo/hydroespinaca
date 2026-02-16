namespace SensorService.Domain.Interfaces;

/// <summary>
/// Interfaz para manejar mensajes recibidos a través del protocolo MQTT
/// desde los nodos ESP32 del sistema hidropónico.
/// </summary>
public interface IMqttMessageHandler
{
    /// <summary>
    /// Procesa un mensaje MQTT recibido en un tópico específico.
    /// </summary>
    /// <param name="topic">Tópico MQTT del mensaje (ej: sensors/data, esp32/telemetry).</param>
    /// <param name="payload">Contenido binario del mensaje MQTT.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task HandleAsync(string topic, byte[] payload, CancellationToken cancellationToken);
}
