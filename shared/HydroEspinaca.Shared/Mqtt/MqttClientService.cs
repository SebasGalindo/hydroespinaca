using HydroEspinaca.Shared.Options;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Protocol;
using System.Text;

namespace HydroEspinaca.Shared.Mqtt;

public class MqttClientService : IMqttClientService
{
    private readonly IMqttClient _client;
    private readonly MqttClientOptions _options;

    public MqttClientService(IOptions<MqttSettings> mqttOptions)
    {
        var config = mqttOptions.Value;

        var factory = new MqttClientFactory();
        _client = factory.CreateMqttClient();

        _options = new MqttClientOptionsBuilder()
           .WithClientId(config.ClientId)
           .WithTcpServer(config.Host, config.Port)
           .WithCredentials(config.Username, config.Password)
           .WithCleanSession()
           .Build();
    }

    public async Task ConnectAsync()
    {
        if (!_client.IsConnected)
            await _client.ConnectAsync(_options);
    }

    public async Task SubscribeAsync(string topic, Func<string, Task> handler)
    {
        _client.ApplicationMessageReceivedAsync += e =>
        {
            var msg = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
            return handler(msg);
        };

        var options = new MqttClientSubscribeOptionsBuilder()
            .WithTopicFilter(x => x
                .WithTopic(topic)
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            ).Build();

        await _client.SubscribeAsync(options);
        await ConnectAsync();
    }

    public async Task PublishAsync(string topic, string payload)
    {
        await ConnectAsync();

        var message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(payload)
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.ExactlyOnce)
            .WithRetainFlag()
            .Build();

        await _client.PublishAsync(message);
    }
}
