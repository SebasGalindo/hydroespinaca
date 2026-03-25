using Microsoft.Extensions.Logging;
using SensorService.Domain.Entities;
using SensorService.Domain.Interfaces;

namespace SensorService.Domain.Services;

/// <summary>
/// Servicio de dominio encargado de resolver (cerrar) alertas activas de sensores
/// cuando los valores vuelven al rango óptimo.
/// </summary>
public class AlertResolutionService : IAlertResolutionService
{
    private readonly ISensorAlertRepository _sensorAlertRepository;
    private readonly IAlertCalculationService _alertCalculationService;
    private readonly ILogger<AlertResolutionService> _logger;

    public AlertResolutionService(
        ISensorAlertRepository sensorAlertRepository,
        IAlertCalculationService alertCalculationService,
        ILogger<AlertResolutionService> logger)
    {
        _sensorAlertRepository = sensorAlertRepository;
        _alertCalculationService = alertCalculationService;
        _logger = logger;
    }

    /// <summary>
    /// Verifica si la lectura actual está dentro del rango óptimo y, de ser así,
    /// resuelve la alerta activa correspondiente marcándola como reconocida.
    /// </summary>
    /// <param name="reading">Lectura actual del sensor.</param>
    /// <param name="variable">Variable con los rangos óptimos.</param>
    /// <param name="timestamp">Momento de la resolución.</param>
    public async Task ResolveOutOfRangeAlertsAsync(Reading reading, Variable variable, DateTime timestamp)
    {
        // Check if reading is now within optimal range
        if (!_alertCalculationService.IsValueWithinOptimalRange(reading.Value, variable))
        {
            return; // Still out of range, don't resolve
        }

        // Resolve any active alert for this variable
        var activeAlert = await _sensorAlertRepository.GetActiveByVariableCodeAsync(reading.VariableCode);

        if (activeAlert != null)
        {
            activeAlert.ResolvedAt = timestamp;
            activeAlert.Acknowledged = true;
            await _sensorAlertRepository.UpdateAsync(activeAlert);

            _logger.LogInformation("✅ Alert {AlertId} resolved for variable {VariableCode}",
                activeAlert.Id, reading.VariableCode);
        }
    }
}