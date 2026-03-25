using HydroEspinaca.Shared.DTOs.Sensors;

namespace SensorService.Application.Interfaces;

/// <summary>
/// Contrato del servicio CRUD de sensores del sistema hidropónico.
/// Gestiona el ciclo de vida completo de los sensores IoT.
/// </summary>
public interface ISensorService
{
    /// <summary>
    /// Obtiene todos los sensores registrados en el sistema.
    /// </summary>
    /// <returns>Lista de todos los sensores.</returns>
    Task<List<SensorDto>> GetAllAsync();

    /// <summary>
    /// Obtiene un sensor por su identificador único.
    /// </summary>
    /// <param name="id">Identificador del sensor.</param>
    /// <returns>DTO del sensor encontrado, o null si no existe.</returns>
    Task<SensorDto?> GetByIdAsync(string id);

    /// <summary>
    /// Crea un nuevo sensor en el sistema.
    /// </summary>
    /// <param name="dto">DTO con los datos para la creación del sensor.</param>
    /// <returns>DTO del sensor creado.</returns>
    Task<SensorDto> CreateAsync(SensorCreateDto dto);

    /// <summary>
    /// Actualiza los datos de un sensor existente.
    /// </summary>
    /// <param name="id">Identificador del sensor a actualizar.</param>
    /// <param name="dto">DTO con los nuevos datos del sensor.</param>
    Task UpdateAsync(string id, SensorUpdateDto dto);

    /// <summary>
    /// Elimina un sensor del sistema.
    /// </summary>
    /// <param name="id">Identificador del sensor a eliminar.</param>
    Task DeleteAsync(string id);
}
