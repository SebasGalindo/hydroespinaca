using HydroEspinaca.Shared.Constants;
using HydroEspinaca.Shared.Mqtt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SensorService.Application.UseCases.ProcessReadingBatch;
using System.Text;

public class SensorMqttWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IMqttClientService _mqttService;
    private readonly ILogger<SensorMqttWorker> _logger;
    private const string SENSOR_TOPIC = MqttTopics.Sensor.ReadingBatches;

    public SensorMqttWorker(IServiceProvider serviceProvider, IMqttClientService mqttService, ILogger<SensorMqttWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _mqttService = mqttService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚀 Starting Sensor MQTT Worker...");

        try
        {
            await _mqttService.SubscribeAsync(SENSOR_TOPIC, async (topic, payload) =>
            {
                await OnSensorMessageReceived(topic, payload, stoppingToken);
            });

            _logger.LogInformation("✅ Sensor MQTT Worker started and subscribed to topic: {Topic}", SENSOR_TOPIC);

            // Keep the service running
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("📄 Sensor MQTT Worker is stopping");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Fatal error in Sensor MQTT Worker");
            throw;
        }
    }

    private async Task OnSensorMessageReceived(string topic, string payload, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return;

        using var scope = _serviceProvider.CreateScope();

        try
        {
            _logger.LogDebug("📨 Received sensor message on topic: {Topic}", topic);

            var dispatcher = scope.ServiceProvider.GetRequiredService<MqttMessageDispatcher>();
            var payloadBytes = Encoding.UTF8.GetBytes(payload);
            await dispatcher.DispatchAsync(topic, payloadBytes, cancellationToken);

            _logger.LogDebug("✅ Successfully processed sensor message from topic: {Topic}", topic);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error processing sensor message from topic: {Topic}", topic);
        }
    }
}
