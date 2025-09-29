using HydroEspinaca.Shared.Enums;
using SensorService.Application.Interfaces.UseCases.ProcessReadingBatch;
using SensorService.Domain.Entities;
using SensorService.Domain.Interfaces;

namespace SensorService.Application.UseCases.ProcessReadingBatch;
public class GenerateAlertsUseCase : IGenerateAlertsUseCase
{
    private readonly IVariableRepository _variableRepository;
    private readonly IAggregateRepository _aggregateRepository;
    private readonly IAlertCalculationService _alertCalculationService;
    private readonly ISensorAlertRepository _sensorAlertRepository;
    private readonly IAlertResolutionService _alertResolutionService;

    public GenerateAlertsUseCase(
        IVariableRepository variableRepository,
        IAggregateRepository aggregateRepository,
        IAlertCalculationService alertCalculationService,
        ISensorAlertRepository sensorAlertRepository,
        IAlertResolutionService alertResolutionService)
    {
        _variableRepository = variableRepository;
        _aggregateRepository = aggregateRepository;
        _alertCalculationService = alertCalculationService;
        _sensorAlertRepository = sensorAlertRepository;
        _alertResolutionService = alertResolutionService;
    }

    public async Task<IEnumerable<SensorAlert>> ExecuteAsync(IEnumerable<Reading> readings, DateTime timestamp)
    {
        var alerts = new List<SensorAlert>();

        foreach (var reading in readings)
        {
            var variable = await _variableRepository.GetByIdAsync(reading.VariableId);
            if (variable == null) continue;

            // Check for luminosity-specific alerts first
            if (_alertCalculationService.IsLuminosityVariable(variable.Name))
            {
                var luminosityAlert = await ProcessLuminosityAlert(reading, variable, timestamp);
                if (luminosityAlert != null)
                    alerts.Add(luminosityAlert);

                // Auto-resolve existing alerts if reading is now within optimal range
                await _alertResolutionService.ResolveOutOfRangeAlertsAsync(reading, variable, timestamp);
                
                // Also resolve any inactive sensor alerts since we received a reading
                await _alertResolutionService.ResolveInactiveSensorAlertsAsync(reading.SensorId, reading.VariableId, timestamp);
            }
            else
            {
                // Standard out of range alerts for non-luminosity variables
                var outOfRangeAlert = await ProcessStandardAlert(reading, variable, timestamp, AlertType.OutOfRange);
                if (outOfRangeAlert != null)
                    alerts.Add(outOfRangeAlert);
                
                // Auto-resolve existing alerts if reading is now within optimal range
                await _alertResolutionService.ResolveOutOfRangeAlertsAsync(reading, variable, timestamp);
                
                // Also resolve any inactive sensor alerts since we received a reading
                await _alertResolutionService.ResolveInactiveSensorAlertsAsync(reading.SensorId, reading.VariableId, timestamp);
            }

            // Anomaly alerts (for all variables)
            var anomalyAlert = await ProcessAnomalyAlert(reading, timestamp);
            if (anomalyAlert != null)
                alerts.Add(anomalyAlert);
        }

        return alerts;
    }

    private async Task<SensorAlert?> ProcessLuminosityAlert(Reading reading, Variable variable, DateTime timestamp)
    {
        var luminosityAlert = _alertCalculationService.CalculateLuminosityAlert(reading, variable, timestamp);
        if (luminosityAlert == null) return null;

        // Check for existing active alert of the same type
        var existingAlert = await _sensorAlertRepository.GetActiveBySensorVariableAndTypeAsync(
            reading.SensorId, reading.VariableId, luminosityAlert.Type);

        if (existingAlert != null)
        {
            // Update existing alert (deduplication)
            existingAlert.Count++;
            existingAlert.LastSeen = timestamp;
            existingAlert.LatestValue = reading.Value;
            await _sensorAlertRepository.UpdateAsync(existingAlert);
            return null; // Don't create a new alert
        }

        return luminosityAlert;
    }

    private async Task<SensorAlert?> ProcessStandardAlert(Reading reading, Variable variable, DateTime timestamp, AlertType alertType)
    {
        var standardAlert = _alertCalculationService.CalculateOutOfRangeAlert(reading, variable, timestamp);
        if (standardAlert == null) return null;

        // Check for existing active alert of the same type
        var existingAlert = await _sensorAlertRepository.GetActiveBySensorVariableAndTypeAsync(
            reading.SensorId, reading.VariableId, alertType);

        if (existingAlert != null)
        {
            // Update existing alert (deduplication)
            existingAlert.Count++;
            existingAlert.LastSeen = timestamp;
            existingAlert.LatestValue = reading.Value;
            await _sensorAlertRepository.UpdateAsync(existingAlert);
            return null; // Don't create a new alert
        }

        return standardAlert;
    }

    private async Task<SensorAlert?> ProcessAnomalyAlert(Reading reading, DateTime timestamp)
    {
        var lastAggregate = await _aggregateRepository
            .GetBySensorAndVariableAsync(reading.SensorId, reading.VariableId,
                DateTime.UtcNow.AddMinutes(-30), DateTime.UtcNow);

        var latest = lastAggregate.OrderByDescending(x => x.Timestamp).FirstOrDefault();
        if (latest == null) return null;

        var anomalyAlert = _alertCalculationService.CalculateAnomalyAlert(reading, latest, timestamp);
        if (anomalyAlert == null) return null;

        // Check for existing active anomaly alert
        var existingAlert = await _sensorAlertRepository.GetActiveBySensorVariableAndTypeAsync(
            reading.SensorId, reading.VariableId, AlertType.Anomaly);

        if (existingAlert != null)
        {
            // Update existing alert (deduplication)
            existingAlert.Count++;
            existingAlert.LastSeen = timestamp;
            existingAlert.LatestValue = reading.Value;
            await _sensorAlertRepository.UpdateAsync(existingAlert);
            return null; // Don't create a new alert
        }

        return anomalyAlert;
    }

}