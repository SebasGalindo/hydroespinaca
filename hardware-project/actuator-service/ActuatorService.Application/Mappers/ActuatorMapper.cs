using HydroEspinaca.Shared.DTOs.Actuator;
using ActuatorService.Domain.Entities;
using HydroEspinaca.Shared.Enums;

namespace ActuatorService.Application.Mappers;
/// <summary>
/// Mapper for converting between Actuator domain entities and DTOs.
/// </summary>
public static class ActuatorMapper
{
    public static Actuator ToEntity(CreateActuatorDto dto)
    {
        if (!Enum.TryParse<ActuatorType>(dto.Type, true, out var actuatorType))
            throw new ArgumentException($"Invalid actuator type: '{dto.Type}'.");

        if (!Enum.TryParse<ActuatorMode>(dto.Mode, true, out var actuatorMode))
            throw new ArgumentException($"Invalid actuator mode: '{dto.Mode}'.");

        return new Actuator
        {
            Esp32Id = dto.Esp32Id,
            Code = dto.Code,
            Type = actuatorType,
            Mode = actuatorMode,
            PhysicalId = dto.PhysicalId,
            Pin = dto.Pin,
            Location = dto.Location,
            PowerConsumptionWatts = dto.PowerConsumptionWatts,
            Status = ActuatorStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static void MapUpdate(UpdateActuatorDto dto, Actuator entity)
    {
        if (!Enum.TryParse<ActuatorStatus>(dto.Status, true, out var status))
            throw new ArgumentException($"Invalid actuator status: '{dto.Status}'.");

        if (!Enum.TryParse<ActuatorMode>(dto.Mode, true, out var mode))
            throw new ArgumentException($"Invalid actuator mode: '{dto.Mode}'.");

        entity.Code = dto.Code;
        entity.Mode = mode;
        entity.Location = dto.Location;
        entity.Pin = dto.Pin;
        entity.PowerConsumptionWatts = dto.PowerConsumptionWatts;
        entity.Status = status;
    }

    public static ActuatorDto ToDto(Actuator x) => new()
    {
        Id = x.Id,
        Esp32Id = x.Esp32Id,
        Code = x.Code,
        Type = x.Type.ToString(),
        Mode = x.Mode.ToString(),
        PhysicalId = x.PhysicalId,
        Pin = x.Pin,
        Location = x.Location,
        Status = x.Status.ToString(),
        PowerConsumptionWatts = x.PowerConsumptionWatts,
        CreatedAt = x.CreatedAt
    };
}
