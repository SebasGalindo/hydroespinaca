using ActuatorService.Domain.Entities;
using ActuatorService.Domain.Interfaces;
using HydroEspinaca.Shared.Mqtt;
using System.Text.Json;

namespace ActuatorService.Infrastructure.Messaging.Mqtt;

public class MqttCommandPublisher : ICommandPublisher
{
    private readonly IMqttClientService _mqttClient;

    public MqttCommandPublisher(IMqttClientService mqttClient)
    {
        _mqttClient = mqttClient;
    }

    public async Task PublishAsync(ActuatorCommand command)
    {
        var topic = $"hydro/{command.Esp32Id}/actuator";

        var payload = new
        {
            commandId = command.Id,
            actuatorId = command.ActuatorId,
            action = command.Action,
            durationMs = command.DurationMs,
            metadata = command.Metadata
        };

        var json = JsonSerializer.Serialize(payload);
        await _mqttClient.PublishAsync(topic, json);
    }
}
