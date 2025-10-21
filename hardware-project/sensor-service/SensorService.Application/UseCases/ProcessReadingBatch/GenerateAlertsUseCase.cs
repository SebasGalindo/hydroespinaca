using HydroEspinaca.Shared.Enums;
using SensorService.Application.Interfaces.UseCases.ProcessReadingBatch;
using SensorService.Domain.Entities;
using SensorService.Domain.Interfaces;

namespace SensorService.Application.UseCases.ProcessReadingBatch;
public class GenerateAlertsUseCase : IGenerateAlertsUseCase
{
    private readonly IVariableRepository _variableRepository;
    private readonly IAlertCalculationService _alertCalculationService;
    private readonly ISensorAlertRepository _sensorAlertRepository;
    private readonly IAlertResolutionService _alertResolutionService;

    public GenerateAlertsUseCase(
        IVariableRepository variableRepository,
        IAlertCalculationService alertCalculationService,
        ISensorAlertRepository sensorAlertRepository,
        IAlertResolutionService alertResolutionService)
    {
        _variableRepository = variableRepository;
        _alertCalculationService = alertCalculationService;
        _sensorAlertRepository = sensorAlertRepository;
        _alertResolutionService = alertResolutionService;
    }

    public async Task<IEnumerable<SensorAlert>> ExecuteAsync(IEnumerable<Reading> readings, DateTime timestamp)
    {
        var alerts = new List<SensorAlert>();

        foreach (var reading in readings)
        {
            var variable = await _variableRepository.GetByCodeAsync(reading.VariableCode);
            if (variable == null) continue;

            // Only process Manual regulation type variables
            if (variable.RegulationType != RegulationType.Manual)
                continue;

            // Calculate alert if value is out of optimal range
            var outOfRangeAlert = _alertCalculationService.CalculateOutOfRangeAlert(reading, variable, timestamp);

            if (outOfRangeAlert != null)
            {
                // Check for existing active alert for this variable
                var existingAlert = await _sensorAlertRepository.GetActiveByVariableCodeAsync(reading.VariableCode);

                if (existingAlert != null)
                {
                    // Update existing alert (deduplication)
                    existingAlert.LastSeen = timestamp;
                    existingAlert.LatestValue = reading.Value;
                    await _sensorAlertRepository.UpdateAsync(existingAlert);
                }
                else
                {
                    // Create new alert
                    alerts.Add(outOfRangeAlert);
                }
            }
            else
            {
                // Value is within optimal range - auto-resolve any existing alerts
                await _alertResolutionService.ResolveOutOfRangeAlertsAsync(reading, variable, timestamp);
            }
        }

        return alerts;
    }
}