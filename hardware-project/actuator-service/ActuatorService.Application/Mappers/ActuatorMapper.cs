namespace ActuatorService.Application.Mappers;

using HydroEspinaca.Shared.DTOs.Actuator;
using ActuatorService.Domain.Entities;
using HydroEspinaca.Shared.Enums;

public static class ActuatorMapper
{
    public static Actuator ToEntity(CreateActuatorDto dto)
    {
        return new Actuator
        {
            Id = Guid.NewGuid().ToString("N"),
            Esp32Id = dto.Esp32Id,
            Name = dto.Name,
            Type = dto.Type,
            PhysicalId = dto.PhysicalId,
            Pin = dto.Pin,
            Location = dto.Location,
            Status = ActuatorStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static void MapUpdate(UpdateActuatorDto dto, Actuator entity)
    {
        entity.Name = dto.Name;
        entity.Location = dto.Location;
        entity.Pin = dto.Pin;
        entity.Status = dto.Status;
    }

    public static ActuatorDto ToDto(Actuator x) => new()
    {
        Id = x.Id,
        Esp32Id = x.Esp32Id,
        Name = x.Name,
        Type = x.Type,
        PhysicalId = x.PhysicalId,
        Pin = x.Pin,
        Location = x.Location,
        Status = x.Status,
        CreatedAt = x.CreatedAt
    };
}
