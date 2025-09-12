using ActuatorService.Domain.Entities;
using System.Text.Json;

namespace ActuatorService.Application.UseCases.Commands;

public static class CommandMessageBuilder
{
    public static (string topic, string payloadJson) Build(ActuatorCommand command)
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

        var payloadJson = JsonSerializer.Serialize(payload);

        return (topic, payloadJson);
    }
}
