namespace SensorService.Domain.Interfaces;
public interface IMqttMessageHandler
{
    Task HandleAsync(string topic, byte[] payload, CancellationToken cancellationToken);
}
