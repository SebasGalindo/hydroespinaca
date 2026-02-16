using SensorService.Application.DTOs.Reading;

namespace SensorService.Application.Interfaces;

/// <summary>
/// Contrato del servicio de consulta de lecturas de sensores hidropónicos.
/// Permite obtener lecturas filtradas por sensor, variable y rango temporal.
/// </summary>
public interface IReadingService
{
    /// <summary>
    /// Obtiene las lecturas de un sensor y variable específicos dentro de un rango de fechas.
    /// </summary>
    /// <param name="sensorId">Identificador del sensor.</param>
    /// <param name="variableId">Identificador de la variable ambiental.</param>
    /// <param name="from">Fecha de inicio del rango de consulta.</param>
    /// <param name="to">Fecha de fin del rango de consulta.</param>
    /// <returns>Lista de lecturas que coinciden con los criterios.</returns>
    Task<List<ReadingDto>> GetBySensorAndVariableAsync(string sensorId, string variableId, DateTime from, DateTime to);
}
