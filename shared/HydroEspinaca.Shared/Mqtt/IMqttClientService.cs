namespace HydroEspinaca.Shared.Mqtt;

public interface IMqttClientService
{
    Task ConnectAsync();
    Task PublishAsync(string topic, string payload);
    Task SubscribeAsync(string topic, Func<string, byte[], Task> handler);
    Task SubscribeAsync(string topic, Func<string, string, Task> textHandler);
}
