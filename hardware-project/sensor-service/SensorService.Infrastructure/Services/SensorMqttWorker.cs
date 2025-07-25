using HydroEspinaca.Shared.Mqtt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SensorService.Application.UseCases.ProcessReadingBatch;

public class SensorMqttWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IMqttClientService _mqttService;

    public SensorMqttWorker(IServiceProvider serviceProvider, IMqttClientService mqttService)
    {
        _serviceProvider = serviceProvider;
        _mqttService = mqttService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<MqttMessageDispatcher>();

        await _mqttService.SubscribeAsync("sensor/", async (topic, payload) =>
        {
            await dispatcher.DispatchAsync(topic, payload, stoppingToken);
        });


        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
