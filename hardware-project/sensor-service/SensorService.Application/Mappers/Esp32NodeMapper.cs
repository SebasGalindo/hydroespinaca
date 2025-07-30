using HydroEspinaca.Shared.DTOs.Esp32;
using HydroEspinaca.Shared.Enums;
using SensorService.Domain.Entities;

namespace SensorService.Application.Mappers;

public static class Esp32NodeMapper
{
    public static Esp32NodeDto ToDto(Esp32Node node) => new()
    {
        Id = node.Id,
        Name = node.Name,
        Location = node.Location,
        LastSeen = node.LastSeen,
        Status = node.Status.ToString()
    };

    public static Esp32Node ToEntity(Esp32NodeCreateDto dto) => new()
    {
        Name = dto.Name,
        Location = dto.Location,
        LastSeen = DateTime.UtcNow,
        Status = Esp32Status.Active
    };
}
