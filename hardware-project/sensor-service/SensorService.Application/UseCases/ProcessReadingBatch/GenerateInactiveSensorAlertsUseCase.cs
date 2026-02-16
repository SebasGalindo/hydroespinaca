using HydroEspinaca.Shared.DTOs.Mqtt;
using Microsoft.Extensions.Logging;
using SensorService.Application.Interfaces.UseCases.ProcessReadingBatch;
using SensorService.Domain.Entities;

namespace SensorService.Application.UseCases.ProcessReadingBatch;

/// <summary>
/// Caso de uso para generar alertas de sensores inactivos.
/// Actualmente deshabilitado: solo las variables de regulación manual generan alertas
/// basadas en el rango óptimo de la variable.
/// </summary>
public class GenerateInactiveSensorAlertsUseCase : IGenerateInactiveSensorAlertsUseCase
{
    private readonly ILogger<GenerateInactiveSensorAlertsUseCase> _logger;

    public GenerateInactiveSensorAlertsUseCase(
        ILogger<GenerateInactiveSensorAlertsUseCase> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Ejecuta la verificación de sensores inactivos (actualmente deshabilitado).
    /// Retorna una colección vacía ya que esta funcionalidad fue reemplazada por alertas de rango óptimo.
    /// </summary>
    /// <param name="dto">DTO del lote de lecturas.</param>
    /// <returns>Colección vacía de alertas.</returns>
    public Task<IEnumerable<SensorAlert>> ExecuteAsync(ReadingBatchDto dto)
    {
        // Disabled: Inactive sensor alerts are no longer generated
        // Only manual regulation type variables generate alerts based on optimal range
        _logger.LogDebug("InactiveSensorAlerts disabled - only manual variables generate alerts");
        return Task.FromResult(Enumerable.Empty<SensorAlert>());
    }
}