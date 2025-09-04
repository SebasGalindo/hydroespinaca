using HydroEspinaca.Shared.Constants;
using HydroEspinaca.Shared.Mqtt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SensorService.Application.Interfaces.UseCases.Esp32Status;
using System.Text.RegularExpressions;

namespace SensorService.Infrastructure.Services;

public class Esp32StatusMqttWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IMqttClientService _mqttService;
    private readonly ILogger<Esp32StatusMqttWorker> _logger;
    
    // Pattern: sensor/{esp32Id}/status
    private const string STATUS_TOPIC_PATTERN = "sensor/+/status";
    private static readonly Regex TopicRegex = new(@"^sensor/([^/]+)/status$", RegexOptions.Compiled);

    public Esp32StatusMqttWorker(
        IServiceProvider serviceProvider, 
        IMqttClientService mqttService, 
        ILogger<Esp32StatusMqttWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _mqttService = mqttService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚀 Starting ESP32 Status MQTT Worker...");

        try
        {
            await _mqttService.SubscribeAsync(STATUS_TOPIC_PATTERN, async (topic, payload) =>
            {
                await OnStatusMessageReceived(topic, payload, stoppingToken);
            });

            _logger.LogInformation("✅ ESP32 Status MQTT Worker started and subscribed to pattern: {Pattern}", STATUS_TOPIC_PATTERN);

            // Keep the service running
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("📄 ESP32 Status MQTT Worker is stopping");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Fatal error in ESP32 Status MQTT Worker");
            throw;
        }
    }

    private async Task OnStatusMessageReceived(string topic, string payload, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return;

        try
        {
            _logger.LogDebug("📨 Received ESP32 status message on topic: {Topic}, payload: {Payload}", topic, payload);

            // Extract esp32Id from topic
            var match = TopicRegex.Match(topic);
            if (!match.Success)
            {
                _logger.LogWarning("⚠️ Invalid topic format: {Topic}", topic);
                return;
            }

            var esp32Id = match.Groups[1].Value;
            var timestamp = DateTime.UtcNow;

            using var scope = _serviceProvider.CreateScope();
            var statusHandler = scope.ServiceProvider.GetRequiredService<IHandleEsp32StatusUseCase>();

            // Process based on payload
            switch (payload.ToLowerInvariant().Trim())
            {
                case "online":
                    await statusHandler.HandleOnlineAsync(esp32Id, timestamp);
                    break;
                
                case "offline":
                    await statusHandler.HandleOfflineAsync(esp32Id, timestamp);
                    break;
                
                default:
                    _logger.LogWarning("⚠️ Unknown status payload: {Payload} for ESP32: {Esp32Id}", payload, esp32Id);
                    break;
            }

            _logger.LogDebug("✅ Successfully processed ESP32 status message for {Esp32Id}: {Status}", esp32Id, payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error processing ESP32 status message from topic: {Topic}", topic);
        }
    }
}