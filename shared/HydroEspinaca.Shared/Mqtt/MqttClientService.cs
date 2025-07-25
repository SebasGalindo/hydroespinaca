using HydroEspinaca.Shared.Options;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Protocol;
using System.Buffers;
using System.Text;

namespace HydroEspinaca.Shared.Mqtt;

public class MqttClientService : IMqttClientService
{
    private readonly IMqttClient _client;
    private readonly MqttClientOptions _options;
    private bool _isHandlerRegistered = false;
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
        if (_client.IsConnected) return;

        try
        {
            await _client.ConnectAsync(_options);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error connecting to MQTT broker", ex);
        }
    }

    public async Task SubscribeAsync(string topic, Func<string, byte[], Task> handler)
    {
        await ConnectAsync();

        if (!_isHandlerRegistered)
        {
            _client.ApplicationMessageReceivedAsync += e =>
            {
                var payload = e.ApplicationMessage.Payload.ToArray();
                var msgTopic = e.ApplicationMessage.Topic;
                return handler(msgTopic, payload);
            };
            _isHandlerRegistered = true;
        }

        var options = new MqttClientSubscribeOptionsBuilder()
            .WithTopicFilter(x => x
                .WithTopic(topic)
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce))
            .Build();

        await _client.SubscribeAsync(options);
    }

    public async Task SubscribeAsync(string topic, Func<string, string, Task> textHandler)
    {
        await SubscribeAsync(topic, (receivedTopic, payload) =>
        {
            var msg = Encoding.UTF8.GetString(payload);
            return textHandler(receivedTopic, msg);
        });
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
