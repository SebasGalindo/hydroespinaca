using Microsoft.Extensions.Logging;
using SensorService.Application.Interfaces;
using SensorService.Domain.Entities;
using SensorService.Domain.Interfaces;
using SensorService.Domain.ValueObjects;

namespace SensorService.Application.Services;

/// <summary>
/// Servicio de aplicación para el procesamiento de alertas críticas del sistema hidropónico.
/// Orquesta la evaluación de lecturas críticas de variables de regulación manual (pH, EC, nivel de agua)
/// y coordina el envío de notificaciones cuando se detectan valores fuera de rango crítico.
/// </summary>
public class CriticalAlertApplicationService : ICriticalAlertApplicationService
{
    private readonly ICriticalReadingEvaluationService _evaluationService;
    private readonly ICriticalAlertNotificationService _notificationService;
    private readonly ISensorRepository _sensorRepository;
    private readonly ILogger<CriticalAlertApplicationService> _logger;

    public CriticalAlertApplicationService(
        ICriticalReadingEvaluationService evaluationService,
        ICriticalAlertNotificationService notificationService,
        ISensorRepository sensorRepository,
        ILogger<CriticalAlertApplicationService> logger)
    {
        _evaluationService = evaluationService;
        _notificationService = notificationService;
        _sensorRepository = sensorRepository;
        _logger = logger;
    }

    /// <summary>
    /// Procesa las alertas críticas para un lote de lecturas de un ESP32.
    /// Flujo: obtiene sensores del ESP32 → evalúa lecturas críticas → verifica si debe enviar alerta → notifica.
    /// </summary>
    /// <param name="esp32Id">Identificador del ESP32 que envió las lecturas.</param>
    /// <param name="timestamp">Marca de tiempo del lote.</param>
    /// <param name="readings">Lecturas a evaluar.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    public async Task ProcessCriticalAlertsAsync(string esp32Id, DateTime timestamp, IEnumerable<Reading> readings, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Processing critical alerts for ESP32: {Esp32Id}", esp32Id);

            // Get all sensors for this ESP32
            var sensors = await _sensorRepository.GetSensorsByEsp32IdAsync(esp32Id, cancellationToken);
            
            // We don't need to filter sensors here anymore since the evaluation service 
            // will dynamically find sensors based on manual variables from the database

            // Evaluate critical readings
            var alertData = await _evaluationService.EvaluateCriticalReadingsAsync(esp32Id, timestamp, readings, sensors, cancellationToken);
            
            if (!alertData.HasAnyAlert)
            {
                _logger.LogDebug("No critical alerts found for ESP32: {Esp32Id}", esp32Id);
                return;
            }

            // Check if we should send alert based on variable codes
            var alertVariableCodes = alertData.AlertReadings.Select(r => r.VariableCode).ToList();
            var shouldSendAlert = await _notificationService.ShouldSendAlertAsync(alertVariableCodes, cancellationToken);

            if (!shouldSendAlert)
            {
                _logger.LogDebug("Alert notification already sent for variables: {Variables}",
                    string.Join(", ", alertVariableCodes));
                return;
            }

            // Send critical alert notification
            await _notificationService.SendCriticalAlertAsync(alertData, cancellationToken);

            // Mark alert as sent
            await _notificationService.MarkAlertAsSentAsync(alertVariableCodes, cancellationToken);

            _logger.LogWarning("Critical alert sent for ESP32: {Esp32Id}, variables: {Variables}",
                esp32Id, string.Join(", ", alertVariableCodes));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing critical alerts for ESP32: {Esp32Id}", esp32Id);
            throw;
        }
    }
}