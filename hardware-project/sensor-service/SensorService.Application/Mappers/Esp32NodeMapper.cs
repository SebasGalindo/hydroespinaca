using SensorService.Application.DTOs.Esp32Node;
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
        Status = node.Status
    };

    public static Esp32Node ToEntity(Esp32NodeCreateDto dto) => new()
    {
        Id = dto.Id,
        Name = dto.Name,
        Location = dto.Location,
        LastSeen = DateTime.UtcNow,
        Status = "active"
    };
}
