using HydroEspinaca.Shared.Enums;
using Microsoft.Extensions.Logging;
using SensorService.Domain.Entities;
using SensorService.Domain.Interfaces;

namespace SensorService.Domain.Services;

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

    public async Task ResolveOutOfRangeAlertsAsync(Reading reading, Variable variable, DateTime timestamp)
    {
        // Check if reading is now within optimal range
        if (!_alertCalculationService.IsValueWithinOptimalRange(reading.Value, variable))
        {
            return; // Still out of range, don't resolve
        }

        // Resolve luminosity alerts if they exist
        await ResolveSpecificAlertTypeAsync(
            reading.SensorId, 
            reading.VariableId, 
            AlertType.LuminosityQuantityInsufficient, 
            "Recovered by reading", 
            timestamp);

        await ResolveSpecificAlertTypeAsync(
            reading.SensorId, 
            reading.VariableId, 
            AlertType.LuminosityQualityInsufficient, 
            "Recovered by reading", 
            timestamp);

        // Resolve standard out-of-range alerts
        await ResolveSpecificAlertTypeAsync(
            reading.SensorId, 
            reading.VariableId, 
            AlertType.OutOfRange, 
            "Recovered by reading", 
            timestamp);
    }

    public async Task ResolveInactiveSensorAlertsAsync(string sensorId, string variableId, DateTime timestamp)
    {
        await ResolveSpecificAlertTypeAsync(
            sensorId, 
            variableId, 
            AlertType.InactiveSensor, 
            "Sensor active again", 
            timestamp);
    }

    public async Task ResolveAlertAsync(SensorAlert alert, string resolutionReason, DateTime timestamp)
    {
        if (alert.ResolvedAt.HasValue)
        {
            _logger.LogDebug("Alert {AlertId} already resolved at {ResolvedAt}", alert.Id, alert.ResolvedAt);
            return;
        }

        alert.ResolvedAt = timestamp;
        alert.Acknowledged = true;
        alert.ResolutionReason = resolutionReason;

        await _sensorAlertRepository.UpdateAsync(alert);

        _logger.LogInformation("✅ Alert {AlertId} resolved: {Type} for sensor {SensorId} - {Reason}", 
            alert.Id, alert.Type, alert.SensorId, resolutionReason);
    }

    private async Task ResolveSpecificAlertTypeAsync(
        string sensorId, 
        string variableId, 
        AlertType alertType, 
        string resolutionReason, 
        DateTime timestamp)
    {
        var activeAlert = await _sensorAlertRepository.GetActiveBySensorVariableAndTypeAsync(
            sensorId, variableId, alertType);

        if (activeAlert != null)
        {
            await ResolveAlertAsync(activeAlert, resolutionReason, timestamp);
        }
    }
}