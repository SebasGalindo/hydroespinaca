using SensorService.Domain.Entities;
using HydroEspinaca.Shared.Enums;

namespace SensorService.Domain.Interfaces;

/// <summary>
/// Repositorio para la persistencia y consulta de variables ambientales del sistema hidropónico.
/// </summary>
public interface IVariableRepository
{
    /// <summary>
    /// Obtiene una variable por su identificador único.
    /// </summary>
    /// <param name="id">Identificador único de la variable.</param>
    /// <returns>La variable encontrada o <c>null</c> si no existe.</returns>
    Task<Variable?> GetByIdAsync(string id);

    /// <summary>
    /// Obtiene una variable por su código único (ej: temperature, ph, tds).
    /// </summary>
    /// <param name="code">Código de la variable.</param>
    /// <returns>La variable encontrada o <c>null</c> si no existe.</returns>
    Task<Variable?> GetByCodeAsync(string code);

    /// <summary>
    /// Obtiene todas las variables ambientales registradas en el sistema.
    /// </summary>
    /// <returns>Lista de todas las variables.</returns>
    Task<List<Variable>> GetAllAsync();

    /// <summary>
    /// Obtiene las variables filtradas por tipo de regulación (manual o automática).
    /// </summary>
    /// <param name="regulationType">Tipo de regulación a filtrar.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Lista de variables con el tipo de regulación especificado.</returns>
    Task<List<Variable>> GetByRegulationTypeAsync(RegulationType regulationType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Crea una nueva variable ambiental en la base de datos.
    /// </summary>
    /// <param name="variable">Variable a persistir.</param>
    Task CreateAsync(Variable variable);

    /// <summary>
    /// Actualiza los datos de una variable existente.
    /// </summary>
    /// <param name="variable">Variable con los datos actualizados.</param>
    Task UpdateAsync(Variable variable);

    /// <summary>
    /// Elimina una variable de la base de datos por su identificador.
    /// </summary>
    /// <param name="id">Identificador único de la variable a eliminar.</param>
    Task DeleteAsync(string id);

    /// <summary>
    /// Obtiene los identificadores que no existen en la base de datos de la lista proporcionada.
    /// </summary>
    /// <param name="ids">Identificadores a verificar.</param>
    /// <returns>Lista de identificadores que no existen en la base de datos.</returns>
    Task<List<string>> GetNonExistingIdsAsync(IEnumerable<string> ids);

    /// <summary>
    /// Obtiene los códigos de variable que no existen en la base de datos de la lista proporcionada.
    /// </summary>
    /// <param name="codes">Códigos de variable a verificar.</param>
    /// <returns>Lista de códigos que no existen en la base de datos.</returns>
    Task<List<string>> GetNonExistingCodesAsync(IEnumerable<string> codes);
}
