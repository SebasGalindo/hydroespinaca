using SensorService.Application.Interfaces.UseCases.ProcessReadingBatch;
using SensorService.Domain.Entities;
using SensorService.Domain.Interfaces;

namespace SensorService.Application.UseCases.ProcessReadingBatch;
public class GenerateAlertsUseCase : IGenerateAlertsUseCase
{
    private readonly IVariableRepository _variableRepository;
    private readonly IAggregateRepository _aggregateRepository;
    private readonly IAlertCalculationService _alertCalculationService;

    public GenerateAlertsUseCase(
        IVariableRepository variableRepository,
        IAggregateRepository aggregateRepository,
        IAlertCalculationService alertCalculationService)
    {
        _variableRepository = variableRepository;
        _aggregateRepository = aggregateRepository;
        _alertCalculationService = alertCalculationService;
    }

    public async Task<IEnumerable<SensorAlert>> ExecuteAsync(IEnumerable<Reading> readings, DateTime timestamp)
    {
        var alerts = new List<SensorAlert>();

        foreach (var reading in readings)
        {
            // Out of range alerts
            var variable = await _variableRepository.GetByIdAsync(reading.VariableId);
            if (variable != null)
            {
                var outOfRangeAlert = _alertCalculationService.CalculateOutOfRangeAlert(reading, variable, timestamp);
                if (outOfRangeAlert != null)
                    alerts.Add(outOfRangeAlert);
            }

            // Anomaly alerts
            var lastAggregate = await _aggregateRepository
                .GetBySensorAndVariableAsync(reading.SensorId, reading.VariableId,
                    DateTime.UtcNow.AddMinutes(-30), DateTime.UtcNow);

            var latest = lastAggregate.OrderByDescending(x => x.Timestamp).FirstOrDefault();
            if (latest != null)
            {
                var anomalyAlert = _alertCalculationService.CalculateAnomalyAlert(reading, latest, timestamp);
                if (anomalyAlert != null)
                    alerts.Add(anomalyAlert);
            }
        }

        return alerts;
    }
}