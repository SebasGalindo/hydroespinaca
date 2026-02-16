using HydroEspinaca.Shared.DTOs.Readings;

namespace SensorService.Application.Interfaces;

/// <summary>
/// Contrato del servicio de últimas lecturas del sistema hidropónico.
/// Proporciona acceso a las lecturas más recientes de todas las variables, enriquecidas con información de la variable.
/// </summary>
public interface ILatestReadingsService
{
    /// <summary>
    /// Obtiene las últimas lecturas de cada variable, enriquecidas con nombre, unidad y rangos óptimos.
    /// </summary>
    /// <returns>DTO con las lecturas más recientes enriquecidas, o null si no hay lecturas disponibles.</returns>
    Task<EnrichedLatestReadingsDto?> GetEnrichedLatestReadingsAsync();
}
