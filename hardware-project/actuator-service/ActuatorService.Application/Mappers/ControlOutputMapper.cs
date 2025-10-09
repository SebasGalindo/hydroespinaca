using ActuatorService.Domain.Entities;
using HydroEspinaca.Shared.DTOs.Actuator;

namespace ActuatorService.Application.Mappers;

public static class ControlOutputMapper
{
    public static ControlOutput ToEntity(CreateControlOutputDto dto)
    {
        return new ControlOutput
        {
            Name = dto.Name,
            Description = dto.Description,
            Unit = dto.Unit,
            ActuatorId = dto.ActuatorId,
            MinValue = dto.MinValue,
            MaxValue = dto.MaxValue,
            LastModified = DateTime.UtcNow
        };
    }

    public static void MapUpdate(UpdateControlOutputDto dto, ControlOutput entity)
    {
        entity.Name = dto.Name;
        entity.Description = dto.Description;
        entity.Unit = dto.Unit;
        entity.ActuatorId = dto.ActuatorId;
        entity.MinValue = dto.MinValue;
        entity.MaxValue = dto.MaxValue;
        entity.LastModified = DateTime.UtcNow;
    }

    public static ControlOutputDto ToDto(ControlOutput entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Description = entity.Description,
        Unit = entity.Unit,
        ActuatorId = entity.ActuatorId,
        MinValue = entity.MinValue,
        MaxValue = entity.MaxValue,
        LastModified = entity.LastModified
    };
}
