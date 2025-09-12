using ActuatorService.Domain.Interfaces;
using HydroEspinaca.Shared.Constants;
using HydroEspinaca.Shared.DTOs.Actuator;
using HydroEspinaca.Shared.Mqtt;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ActuatorService.Infrastructure.Messaging.Mqtt;

public class MqttRoutineCommandPublisher : IRoutineCommandPublisher
{
    private readonly IMqttClientService _mqttClient;
    private readonly ILogger<MqttRoutineCommandPublisher> _logger;
    private const string JOB_SCHEDULE_TOPIC = MqttTopics.Actuator.JobSchedule;

    public MqttRoutineCommandPublisher(IMqttClientService mqttClient, ILogger<MqttRoutineCommandPublisher> logger)
    {
        _mqttClient = mqttClient;
        _logger = logger;
    }

    public async Task PublishJobScheduleAsync(JobScheduleDto jobSchedule)
    {
        if (jobSchedule == null)
        {
            _logger.LogError("❌ Cannot publish null job schedule");
            throw new ArgumentNullException(nameof(jobSchedule));
        }

        if (string.IsNullOrEmpty(jobSchedule.Esp32Id))
        {
            _logger.LogError("❌ Cannot publish job schedule with null or empty ESP32 ID");
            throw new ArgumentException("ESP32 ID cannot be null or empty", nameof(jobSchedule));
        }

        try
        {
            _logger.LogDebug("📤 Preparing to publish job schedule for ESP32: {Esp32Id}", jobSchedule.Esp32Id);
            
            var routineCount = jobSchedule.JobSchedule?.Sum(channel => channel.Queue?.Count ?? 0) ?? 0;
            var channelCount = jobSchedule.JobSchedule?.Count ?? 0;

            var jsonPayload = JsonSerializer.Serialize(jobSchedule, JsonConstants.SerializerOptions.CamelCase);
            var payloadSizeBytes = System.Text.Encoding.UTF8.GetByteCount(jsonPayload);
            
            _logger.LogInformation("📊 MQTT payload size: {PayloadSize} bytes for ESP32 {Esp32Id}", payloadSizeBytes, jobSchedule.Esp32Id);
            
            await _mqttClient.PublishAsync(JOB_SCHEDULE_TOPIC, jsonPayload);
            
            _logger.LogInformation("✅ Published job schedule for ESP32 {Esp32Id} to topic {Topic} - {ChannelCount} channels, {RoutineCount} total routines", 
                jobSchedule.Esp32Id, JOB_SCHEDULE_TOPIC, channelCount, routineCount);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "❌ Failed to serialize job schedule for ESP32 {Esp32Id}", jobSchedule.Esp32Id);
            throw new InvalidOperationException($"Failed to serialize job schedule for ESP32 {jobSchedule.Esp32Id}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to publish job schedule for ESP32 {Esp32Id} to topic {Topic}", 
                jobSchedule.Esp32Id, JOB_SCHEDULE_TOPIC);
            throw new InvalidOperationException($"Failed to publish job schedule for ESP32 {jobSchedule.Esp32Id}", ex);
        }
    }
}