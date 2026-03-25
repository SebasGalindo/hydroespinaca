using HydroEspinaca.Shared.DTOs.Esp32;
using HydroEspinaca.Shared.Enums;
using SensorService.Domain.Entities;

namespace SensorService.Application.Mappers;

/// <summary>
/// Mapper estático para convertir entre entidades de dominio Esp32Node y sus DTOs correspondientes.
/// Gestiona la conversión de nodos microcontroladores ESP32 del sistema IoT hidropónico.
/// </summary>
public static class Esp32NodeMapper
{
    /// <summary>
    /// Convierte una entidad de dominio Esp32Node a su DTO de respuesta.
    /// </summary>
    /// <param name="node">Entidad de dominio Esp32Node.</param>
    /// <returns>DTO con los datos del nodo ESP32 para la respuesta API.</returns>
    public static Esp32NodeDto ToDto(Esp32Node node) => new()
    {
        Id = node.Id,
        Name = node.Name,
        Location = node.Location,
        LastSeen = node.LastSeen,
        Status = node.Status.ToString()
    };

    /// <summary>
    /// Convierte un DTO de creación de nodo ESP32 a una entidad de dominio con estado activo por defecto.
    /// </summary>
    /// <param name="dto">DTO con los datos de creación del nodo.</param>
    /// <returns>Nueva entidad de dominio Esp32Node.</returns>
    public static Esp32Node ToEntity(Esp32NodeCreateDto dto) => new()
    {
        Name = dto.Name,
        Location = dto.Location,
        LastSeen = DateTime.UtcNow,
        Status = Esp32Status.Active
    };
}
