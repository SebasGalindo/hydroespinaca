using SensorService.Domain.Entities;

namespace SensorService.Application.Interfaces.UseCases.ProcessReadingBatch;

/// <summary>
/// Contrato del caso de uso para generar alertas basadas en lecturas fuera del rango óptimo.
/// Evalúa las lecturas de variables de regulación manual contra los rangos óptimos configurados.
/// </summary>
public interface IGenerateAlertsUseCase
{
    /// <summary>
    /// Evalúa las lecturas y genera alertas cuando los valores están fuera del rango óptimo.
    /// Incluye deduplicación de alertas activas y resolución automática cuando los valores vuelven al rango.
    /// </summary>
    /// <param name="readings">Colección de lecturas a evaluar.</param>
    /// <param name="timestamp">Marca de tiempo de la evaluación.</param>
    /// <returns>Colección de nuevas alertas generadas.</returns>
    Task<IEnumerable<SensorAlert>> ExecuteAsync(IEnumerable<Reading> readings, DateTime timestamp);
}