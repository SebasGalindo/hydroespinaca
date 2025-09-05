using HydroEspinaca.Shared.Constants;
using HydroEspinaca.Shared.Enums;
using SensorService.Domain.Entities;
using SensorService.Domain.Interfaces;

namespace SensorService.Domain.Services;
public class AlertCalculationService : IAlertCalculationService
{
    public SensorAlert? CalculateOutOfRangeAlert(Reading reading, Variable variable, DateTime timestamp)
    {
        var value = reading.Value;

        // Caso 1 (Error): Valor fuera del rango físico - sensor posiblemente roto
        if (value < variable.PhysicalMin || value > variable.PhysicalMax)
        {
            var physicalThreshold = value < variable.PhysicalMin ? variable.PhysicalMin : variable.PhysicalMax;
            return new SensorAlert
            {
                Type = AlertType.OutOfRange,
                SensorId = reading.SensorId,
                Value = value,
                Threshold = physicalThreshold,
                Timestamp = timestamp,
                Severity = AlertSeverity.Critical,
                Message = $"Valor {value} fuera del rango físico permitido [{variable.PhysicalMin} - {variable.PhysicalMax}]",
                Acknowledged = false
            };
        }

        // Caso 2 (Warning): Dentro del rango físico pero fuera del óptimo - condiciones subóptimas
        if (value < variable.OptimalMin || value > variable.OptimalMax)
        {
            var optimalThreshold = value < variable.OptimalMin ? variable.OptimalMin : variable.OptimalMax;
            return new SensorAlert
            {
                Type = AlertType.OutOfRange,
                SensorId = reading.SensorId,
                Value = value,
                Threshold = optimalThreshold,
                Timestamp = timestamp,
                Severity = AlertSeverity.Warning,
                Message = $"Valor {value} fuera del rango óptimo [{variable.OptimalMin} - {variable.OptimalMax}]",
                Acknowledged = false
            };
        }

        // Caso 3 (OK): Dentro del rango óptimo - no generar alerta
        return null;
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