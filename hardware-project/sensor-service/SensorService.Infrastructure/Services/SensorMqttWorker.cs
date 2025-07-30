using HydroEspinaca.Shared.Mqtt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SensorService.Application.UseCases.ProcessReadingBatch;

public class SensorMqttWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IMqttClientService _mqttService;
    private readonly ILogger<SensorMqttWorker> _logger;

    public SensorMqttWorker(IServiceProvider serviceProvider, IMqttClientService mqttService, ILogger<SensorMqttWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _mqttService = mqttService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("?? Starting MQTT worker...");

        using var scope = _serviceProvider.CreateScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<MqttMessageDispatcher>();

        await _mqttService.SubscribeAsync("sensor/#", async (topic, payload) =>
        {
            _logger.LogInformation("Dispatching MQTT message. Topic: {Topic}", topic);
            await dispatcher.DispatchAsync(topic, payload, stoppingToken);
        });

        _logger.LogInformation("MQTT worker started and subscribed. Waiting for messages...");

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
