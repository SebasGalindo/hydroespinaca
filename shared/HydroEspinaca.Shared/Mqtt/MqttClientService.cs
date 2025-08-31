using HydroEspinaca.Shared.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Protocol;
using System.Buffers;
using System.Collections.Concurrent;
using System.Text;

namespace HydroEspinaca.Shared.Mqtt;

public class MqttClientService : IMqttClientService
{
    private readonly IMqttClient _client;
    private readonly MqttClientOptions _options;
    private bool _isHandlerRegistered;
    private readonly object _handlerLock = new();
    private readonly SemaphoreSlim _connectLock = new SemaphoreSlim(1, 1);
    private readonly ILogger<MqttClientService> _logger;
    private readonly string _instanceId = Guid.NewGuid().ToString("N");
    private readonly ConcurrentDictionary<string, byte> _subscribedTopics = new();
    private readonly ConcurrentDictionary<string, Func<string, byte[], Task>> _topicHandlers = new();

    private static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(10);

    public MqttClientService(IOptions<MqttSettings> mqttOptions, ILogger<MqttClientService> logger)
    {
        _logger = logger;
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
        if (_client.IsConnected)
        {
            _logger.LogInformation("[{Instance}] MQTT client already connected.", _instanceId);
            return;
        }

        await _connectLock.WaitAsync();
        try
        {
            // doble-check dentro del lock
            if (_client.IsConnected)
            {
                _logger.LogInformation("[{Instance}] MQTT client already connected (post-lock).", _instanceId);
                return;
            }

            try
            {
                _logger.LogInformation("[{Instance}] Connecting to MQTT broker...", _instanceId);
                using (var cts = new CancellationTokenSource(ConnectTimeout))
                {
                    MqttClientConnectResult mqttClientConnectResult = await _client.ConnectAsync(_options, cts.Token);
                    if (_client.IsConnected)
                    {
                        _logger.LogInformation("[{Instance}] Connected to MQTT broker. Result: {Result}", _instanceId, mqttClientConnectResult?.ResultCode);
                        return;
                    }

                    _logger.LogWarning("[{Instance}] Failed to connect to MQTT broker. Result: {Reason}", _instanceId, mqttClientConnectResult?.ResultCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[{Instance}] Error connecting to MQTT broker.", _instanceId);
                throw new InvalidOperationException("Error connecting to MQTT broker", ex);
            }
        }
        finally
        {
            _connectLock.Release();
        }
    }

    public async Task SubscribeAsync(string topic, Func<string, byte[], Task> handler)
    {
        await ConnectAsync();

        // Store the topic-specific handler
        _topicHandlers.TryAdd(topic, handler);

        // Protege el registro del handler contra race conditions
        lock (_handlerLock)
        {
            if (!_isHandlerRegistered)
            {
                _client.ApplicationMessageReceivedAsync += async e =>
                {
                    ReadOnlySequence<byte> sequence = e.ApplicationMessage.Payload;
                    byte[] array = BuffersExtensions.ToArray(in sequence);
                    string topicReceived = e.ApplicationMessage.Topic;
                    _logger.LogInformation("[{Instance}] MQTT message received. Topic: {Topic}, Payload: {Payload}", _instanceId, topicReceived, Encoding.UTF8.GetString(array));
                    
                    // Route message to the correct topic handler
                    if (_topicHandlers.TryGetValue(topicReceived, out var topicHandler))
                    {
                        try
                        {
                            await topicHandler(topicReceived, array).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "[{Instance}] Error in message handler for topic: {Topic}", _instanceId, topicReceived);
                            // no rethrow: evita que excepciones asincrónicas rompan el loop del cliente
                        }
                    }
                    else
                    {
                        _logger.LogWarning("[{Instance}] No handler found for topic: {Topic}", _instanceId, topicReceived);
                    }
                };

                _isHandlerRegistered = true;
                _logger.LogInformation("[{Instance}] Registered ApplicationMessageReceived handler.", _instanceId);
            }
            else
            {
                _logger.LogDebug("[{Instance}] Handler already registered - using topic-based routing.", _instanceId);
            }
        }

        // Evita suscripciones repetidas
        if (!_subscribedTopics.TryAdd(topic, 0))
        {
            _logger.LogDebug("[{Instance}] Already subscribed to topic {Topic} - skipping subscribe call", _instanceId, topic);
            return;
        }

        var options = new MqttClientSubscribeOptionsBuilder().WithTopicFilter(x =>
        {
            x.WithTopic(topic).WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce);
        }).Build();

        try
        {
            await _client.SubscribeAsync(options);
            _logger.LogInformation("[{Instance}] Subscribed to MQTT topic: {Topic}", _instanceId, topic);
        }
        catch (Exception exception)
        {
            _subscribedTopics.TryRemove(topic, out _); // rollback on failure
            _logger.LogError(exception, "[{Instance}] Failed to subscribe to topic: {Topic}", _instanceId, topic);
            throw;
        }
    }

    public async Task SubscribeAsync(string topic, Func<string, string, Task> textHandler)
    {
        await SubscribeAsync(topic, (receivedTopic, payload) =>
        {
            string @string = Encoding.UTF8.GetString(payload);
            return textHandler(receivedTopic, @string);
        });
    }

    public async Task PublishAsync(string topic, string payload)
    {
        await ConnectAsync();
        MqttApplicationMessage applicationMessage = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(payload)
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.ExactlyOnce)
            .WithRetainFlag()
            .Build();
        await _client.PublishAsync(applicationMessage);
    }
}
