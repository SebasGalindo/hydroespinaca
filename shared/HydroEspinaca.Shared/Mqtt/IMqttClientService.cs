namespace HydroEspinaca.Shared.Mqtt;

public interface IMqttClientService
{
    Task ConnectAsync();
    Task PublishAsync(string topic, string payload);
    Task SubscribeAsync(string topic, Func<string, Task> messageHandler);
}
