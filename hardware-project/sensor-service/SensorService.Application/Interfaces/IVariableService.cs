using HydroEspinaca.Shared.DTOs.Variables;

namespace SensorService.Application.Interfaces;

/// <summary>
/// Contrato del servicio CRUD de variables ambientales del sistema hidropónico.
/// Gestiona las variables que los sensores pueden medir (temperatura, pH, EC, etc.).
/// </summary>
public interface IVariableService
{
    /// <summary>
    /// Obtiene todas las variables ambientales registradas.
    /// </summary>
    /// <returns>Lista de todas las variables.</returns>
    Task<List<VariableDto>> GetAllAsync();

    /// <summary>
    /// Obtiene una variable por su identificador único.
    /// </summary>
    /// <param name="id">Identificador de la variable.</param>
    /// <returns>DTO de la variable encontrada, o null si no existe.</returns>
    Task<VariableDto?> GetByIdAsync(string id);

    /// <summary>
    /// Crea una nueva variable ambiental en el sistema.
    /// </summary>
    /// <param name="dto">DTO con los datos para la creación de la variable.</param>
    Task AddAsync(VariableCreateDto dto);

    /// <summary>
    /// Actualiza los datos de una variable ambiental existente.
    /// </summary>
    /// <param name="id">Identificador de la variable a actualizar.</param>
    /// <param name="dto">DTO con los nuevos datos de la variable.</param>
    Task UpdateAsync(string id, VariableUpdateDto dto);

    /// <summary>
    /// Elimina una variable ambiental del sistema.
    /// </summary>
    /// <param name="id">Identificador de la variable a eliminar.</param>
    Task DeleteAsync(string id);
}
