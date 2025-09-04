using HydroEspinaca.Shared.Constants;
using HydroEspinaca.Shared.Mqtt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SensorService.Application.DTOs.Esp32Status;
using SensorService.Application.Interfaces.UseCases.Esp32Status;
using System.Text.Json;
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

            var topicEsp32Id = match.Groups[1].Value;

            using var scope = _serviceProvider.CreateScope();
            var statusHandler = scope.ServiceProvider.GetRequiredService<IHandleEsp32StatusUseCase>();

            // Try to parse JSON payload first
            Esp32StatusPayloadDto? statusPayload = null;
            try
            {
                statusPayload = JsonSerializer.Deserialize<Esp32StatusPayloadDto>(payload, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    AllowTrailingCommas = true
                });

                // Validate that esp32Id in payload matches topic (if provided)
                if (!string.IsNullOrWhiteSpace(statusPayload.Esp32Id) && 
                    !string.Equals(statusPayload.Esp32Id, topicEsp32Id, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("⚠️ ESP32 ID mismatch - Topic: {TopicId}, Payload: {PayloadId}", 
                        topicEsp32Id, statusPayload.Esp32Id);
                }

                // Use topic esp32Id if payload doesn't have one
                if (string.IsNullOrWhiteSpace(statusPayload.Esp32Id))
                {
                    statusPayload.Esp32Id = topicEsp32Id;
                }
            }
            catch (JsonException jsonEx)
            {
                _logger.LogDebug("📄 Payload no es JSON válido, intentando parsing como texto plano: {Error}", jsonEx.Message);
                
                // Fallback to plain text parsing for backward compatibility
                var plainTextStatus = payload.ToLowerInvariant().Trim();
                statusPayload = new Esp32StatusPayloadDto
                {
                    Esp32Id = topicEsp32Id,
                    Status = plainTextStatus,
                    Timestamp = DateTime.UtcNow
                };
            }

            if (statusPayload != null)
            {
                await statusHandler.HandleStatusPayloadAsync(statusPayload);
                _logger.LogDebug("✅ Successfully processed ESP32 status message for {Esp32Id}: {Status}", 
                    statusPayload.Esp32Id, statusPayload.Status);
            }
            else
            {
                _logger.LogWarning("⚠️ No se pudo procesar payload para topic: {Topic}", topic);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error processing ESP32 status message from topic: {Topic}", topic);
        }
    }
}