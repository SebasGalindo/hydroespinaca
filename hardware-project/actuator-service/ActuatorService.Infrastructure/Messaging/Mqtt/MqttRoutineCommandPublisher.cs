using ActuatorService.Domain.Interfaces;
using HydroEspinaca.Shared.DTOs.Actuator;
using HydroEspinaca.Shared.Mqtt;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ActuatorService.Infrastructure.Messaging.Mqtt;

public class MqttRoutineCommandPublisher : IRoutineCommandPublisher
{
    private readonly IMqttClientService _mqttClient;
    private readonly ILogger<MqttRoutineCommandPublisher> _logger;
    private const string JOB_SCHEDULE_TOPIC = "actuator/job/schedule";

    public MqttRoutineCommandPublisher(IMqttClientService mqttClient, ILogger<MqttRoutineCommandPublisher> logger)
    {
        _mqttClient = mqttClient;
        _logger = logger;
    }

    public async Task PublishJobScheduleAsync(JobScheduleDto jobSchedule)
    {
        var jsonPayload = JsonSerializer.Serialize(jobSchedule, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        
        await _mqttClient.PublishAsync(JOB_SCHEDULE_TOPIC, jsonPayload);
        _logger.LogInformation("Published job schedule for ESP32 {Esp32Id} to {Topic}", 
            jobSchedule.Esp32Id, JOB_SCHEDULE_TOPIC);
    }
}