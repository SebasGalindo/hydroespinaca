using HydroEspinaca.Shared.Constants;
using HydroEspinaca.Shared.Enums;
using SensorService.Domain.Entities;
using SensorService.Domain.Interfaces;

namespace SensorService.Domain.Services;
public class AlertCalculationService : IAlertCalculationService
{
    public SensorAlert? CalculateOutOfRangeAlert(Reading reading, Variable variable, DateTime timestamp)
    {
        if (reading.Value >= variable.MinValue && reading.Value <= variable.MaxValue)
            return null;

        var threshold = reading.Value < variable.MinValue ? variable.MinValue : variable.MaxValue;
        return new SensorAlert
        {
            Type = AlertType.OutOfRange,
            SensorId = reading.SensorId,
            Value = reading.Value,
            Threshold = threshold,
            Timestamp = timestamp,
            Severity = AlertSeverity.Warning,
            Message = $"Valor {reading.Value} fuera del rango permitido [{variable.MinValue} - {variable.MaxValue}]",
            Acknowledged = false
        };
    }

    public SensorAlert? CalculateAnomalyAlert(Reading reading, Aggregate latestAggregate, DateTime timestamp)
    {
        var diff = Math.Abs(reading.Value - latestAggregate.Avg);
        var threshold = latestAggregate.Avg * AlertRules.AnomalyThresholdPercent;
        if (diff <= threshold)
            return null;

        return new SensorAlert
        {
            SensorId = reading.SensorId,
            Type = AlertType.Anomaly,
            Value = reading.Value,
            Threshold = latestAggregate.Avg,
            Timestamp = timestamp,
            Severity = AlertSeverity.Info,
            Message = $"Valor anómalo: {reading.Value} difiere significativamente del promedio anterior {latestAggregate.Avg:F2}",
            Acknowledged = false
        };
    }
}