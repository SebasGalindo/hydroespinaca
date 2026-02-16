using SensorService.Domain.Entities;
using HydroEspinaca.Shared.DTOs.Analytics;

namespace SensorService.Domain.Interfaces;

/// <summary>
/// Repositorio para la persistencia y consulta de datos agregados de lecturas de sensores.
/// </summary>
public interface IAggregateRepository
{
    /// <summary>
    /// Crea un nuevo registro de datos agregados en la base de datos.
    /// </summary>
    /// <param name="aggregate">Entidad de agregado a persistir.</param>
    Task CreateAsync(Aggregate aggregate);

    /// <summary>
    /// Obtiene los datos agregados de un sensor y variable específicos dentro de un rango de fechas.
    /// </summary>
    /// <param name="sensorCode">Código del sensor.</param>
    /// <param name="variableCode">Código de la variable ambiental.</param>
    /// <param name="from">Fecha de inicio del rango (inclusiva).</param>
    /// <param name="to">Fecha de fin del rango (inclusiva).</param>
    /// <returns>Lista de agregados que coinciden con los criterios de búsqueda.</returns>
    Task<List<Aggregate>> GetBySensorAndVariableAsync(string sensorCode, string variableCode, DateTime from, DateTime to);

    /// <summary>
    /// Obtiene un agregado específico por sensor, variable y marca temporal exacta.
    /// </summary>
    /// <param name="sensorCode">Código del sensor.</param>
    /// <param name="variableCode">Código de la variable ambiental.</param>
    /// <param name="timestamp">Marca temporal exacta del agregado.</param>
    /// <returns>El agregado encontrado o <c>null</c> si no existe.</returns>
    Task<Aggregate?> GetBySensorAndVariableAndTimestampAsync(string sensorCode, string variableCode, DateTime timestamp);

    /// <summary>
    /// Obtiene agregados ambientales agrupados por variable para análisis y reportes BI.
    /// </summary>
    /// <param name="request">Parámetros de la solicitud de análisis ambiental.</param>
    /// <returns>Diccionario con código de variable como clave y lista de agregados como valor.</returns>
    Task<Dictionary<string, List<Aggregate>>> GetEnvironmentalAggregatesAsync(EnvironmentalAnalyticsRequest request);
}
