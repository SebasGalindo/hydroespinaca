using SensorService.Application.DTOs.Aggregate;
using HydroEspinaca.Shared.DTOs.Analytics;

namespace SensorService.Application.Interfaces;

/// <summary>
/// Contrato del servicio de agregación de datos de sensores hidropónicos.
/// Proporciona consultas de datos agregados por sensor/variable y análisis ambiental.
/// </summary>
public interface IAggregateService
{
    /// <summary>
    /// Obtiene los datos agregados para un sensor y variable específicos dentro de un rango de fechas.
    /// </summary>
    /// <param name="sensorId">Identificador del sensor.</param>
    /// <param name="variableId">Identificador de la variable ambiental.</param>
    /// <param name="from">Fecha de inicio del rango de consulta.</param>
    /// <param name="to">Fecha de fin del rango de consulta.</param>
    /// <returns>Lista de datos agregados que coinciden con los criterios de búsqueda.</returns>
    Task<List<AggregateDto>> GetBySensorAndVariableAsync(string sensorId, string variableId, DateTime from, DateTime to);

    /// <summary>
    /// Obtiene los agregados ambientales con resumen, tendencia y variabilidad según la vista solicitada.
    /// </summary>
    /// <param name="request">Solicitud con rango de fechas, tipo de vista (horaria, diaria, semanal, mensual) y filtros.</param>
    /// <returns>Respuesta con las variables ambientales agregadas incluyendo resumen, tendencia y variabilidad.</returns>
    Task<EnvironmentalAggregatesResponse> GetEnvironmentalAggregatesAsync(EnvironmentalAnalyticsRequest request);
}
