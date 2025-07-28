using ActuatorService.Domain.Entities;
using HydroEspinaca.Shared.DTOs.Actuator;
using HydroEspinaca.Shared.Enums;

namespace ActuatorService.Application.Mappers;

public static class CommandMapper
{
    public static ActuatorCommand ToEntity(CreateCommandDto dto, string? userId)
    {
        if (!Enum.TryParse<TriggerType>(dto.Trigger, true, out var actuatorType))
            throw new ArgumentException($"Invalid trigger type: '{dto.Trigger}'.");

        return new ActuatorCommand
        {
            ActuatorId = dto.ActuatorId,
            Esp32Id = dto.Esp32Id,
            Action = dto.Action,
            DurationMs = dto.DurationMs,
            Trigger = actuatorType,
            RoutineId = dto.RoutineId,
            RoutineStepOrder = dto.RoutineStepOrder,
            UserId = userId,
            Metadata = dto.Metadata is null ? null : new CommandMetadata
            {
                Source = dto.Metadata.Source,
                FuzzyRule = dto.Metadata.FuzzyRule,
                Inputs = dto.Metadata.Inputs
            },
            Timestamp = DateTime.UtcNow
        };
    }
    public static ActuatorCommandDto ToDto(ActuatorCommand entity)
    {
        return new ActuatorCommandDto
        {
            Id = entity.Id,
            ActuatorId = entity.ActuatorId,
            Esp32Id = entity.Esp32Id,
            Action = entity.Action,
            DurationMs = entity.DurationMs,
            Trigger = entity.Trigger.ToString(),
            RoutineId = entity.RoutineId,
            RoutineStepOrder = entity.RoutineStepOrder,
            Metadata = ToDto(entity.Metadata),
            Timestamp = entity.Timestamp
        };
    }
    private static CommandMetadataDto? ToDto(CommandMetadata? metadata)
    {
        if (metadata is null) return null;

        return new CommandMetadataDto
        {
            Source = metadata.Source,
            FuzzyRule = metadata.FuzzyRule,
            Inputs = metadata.Inputs
        };
    }
}
