using HydroEspinaca.Shared.DTOs.Readings;
using SensorService.Application.Interfaces;
using SensorService.Domain.Interfaces;

namespace SensorService.Application.Services;

/// <summary>
/// Servicio de aplicación para obtener las últimas lecturas de todas las variables del sistema hidropónico.
/// Enriquece las lecturas con información de la variable (nombre, unidad, rangos óptimos)
/// para presentación directa en el dashboard del frontend.
/// </summary>
public class LatestReadingsService : ILatestReadingsService
{
    private readonly IReadingRepository _readingRepo;
    private readonly IVariableRepository _variableRepo;

    public LatestReadingsService(IReadingRepository readingRepo, IVariableRepository variableRepo)
    {
        _readingRepo = readingRepo;
        _variableRepo = variableRepo;
    }

    /// <summary>
    /// Obtiene las últimas lecturas de cada variable, enriquecidas con nombre, unidad y rangos óptimos.
    /// Cruza datos de lecturas recientes con la información de variables para proporcionar contexto completo.
    /// </summary>
    /// <returns>DTO con las lecturas más recientes enriquecidas, o null si no hay lecturas.</returns>
    public async Task<EnrichedLatestReadingsDto?> GetEnrichedLatestReadingsAsync()
    {
        // Get latest readings from readings collection
        var readings = await _readingRepo.GetLatestReadingsByVariableAsync();

        if (readings == null || !readings.Any())
            return null;

        // Get all variables to enrich the readings
        var variables = await _variableRepo.GetAllAsync();
        var variableDictionary = variables.ToDictionary(v => v.Code, StringComparer.OrdinalIgnoreCase);

        // Get the most recent timestamp from all readings
        var latestTimestamp = readings.Max(r => r.Timestamp);

        // Enrich readings with variable information
        var enrichedReadings = readings
            .Where(r => variableDictionary.ContainsKey(r.VariableCode))
            .Select(r =>
            {
                var variable = variableDictionary[r.VariableCode];
                return new EnrichedReadingItemDto
                {
                    Name = variable.Name,
                    Value = r.Value,
                    Unit = variable.Unit,
                    OptimalMin = variable.OptimalMin,
                    OptimalMax = variable.OptimalMax
                };
            })
            .ToList();

        return new EnrichedLatestReadingsDto
        {
            Timestamp = latestTimestamp,
            Readings = enrichedReadings
        };
    }
}
