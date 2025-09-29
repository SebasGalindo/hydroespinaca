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
                VariableId = reading.VariableId,
                Value = value,
                Threshold = physicalThreshold,
                Timestamp = timestamp,
                Severity = AlertSeverity.Critical,
                Message = $"Valor {value} fuera del rango físico permitido [{variable.PhysicalMin} - {variable.PhysicalMax}]",
                Acknowledged = false
            };
        }

        // Caso 2 (Warning): Dentro del rango físico pero fuera del óptimo - condiciones subóptimas
        // Check if value is below OptimalMin
        if (value < variable.OptimalMin)
        {
            return new SensorAlert
            {
                Type = AlertType.OutOfRange,
                SensorId = reading.SensorId,
                VariableId = reading.VariableId,
                Value = value,
                Threshold = variable.OptimalMin,
                Timestamp = timestamp,
                Severity = AlertSeverity.Warning,
                Message = GetOptimalRangeMessage(value, variable),
                Acknowledged = false
            };
        }

        // Check if value is above OptimalMax (only if OptimalMax is defined)
        if (variable.OptimalMax.HasValue && value > variable.OptimalMax.Value)
        {
            return new SensorAlert
            {
                Type = AlertType.OutOfRange,
                SensorId = reading.SensorId,
                VariableId = reading.VariableId,
                Value = value,
                Threshold = variable.OptimalMax.Value,
                Timestamp = timestamp,
                Severity = AlertSeverity.Warning,
                Message = GetOptimalRangeMessage(value, variable),
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
            VariableId = reading.VariableId,
            Type = AlertType.Anomaly,
            Value = reading.Value,
            Threshold = latestAggregate.Avg,
            Timestamp = timestamp,
            Severity = AlertSeverity.Info,
            Message = $"Valor anómalo: {reading.Value} difiere significativamente del promedio anterior {latestAggregate.Avg:F2}",
            Acknowledged = false
        };
    }

    public SensorAlert? CalculateLuminosityAlert(Reading reading, Variable variable, DateTime timestamp)
    {
        var value = reading.Value;

        if (IsLuminosityClear(variable.Name))
        {
            // Luminosity Clear (cantidad): siempre validar si llega
            if (value < variable.OptimalMin)
            {
                return new SensorAlert
                {
                    Type = AlertType.LuminosityQuantityInsufficient,
                    SensorId = reading.SensorId,
                    VariableId = reading.VariableId,
                    Value = value,
                    Threshold = variable.OptimalMin,
                    Timestamp = timestamp,
                    Severity = AlertSeverity.Warning,
                    Message = "Cantidad de luz insuficiente",
                    Acknowledged = false
                };
            }
        }
        else if (IsLuminosityIndex(variable.Name))
        {
            // Luminosity Index (calidad): validar solo si llega (firmware ya filtró)
            if (value < variable.OptimalMin)
            {
                return new SensorAlert
                {
                    Type = AlertType.LuminosityQualityInsufficient,
                    SensorId = reading.SensorId,
                    VariableId = reading.VariableId,
                    Value = value,
                    Threshold = variable.OptimalMin,
                    Timestamp = timestamp,
                    Severity = AlertSeverity.Warning,
                    Message = "Calidad de luz insuficiente",
                    Acknowledged = false
                };
            }
        }

        return null;
    }

    public bool IsLuminosityVariable(string variableName)
    {
        var normalizedName = variableName.ToLowerInvariant();
        return normalizedName.Contains("light") || 
               normalizedName.Contains("luz") || 
               normalizedName.Contains("luminosity");
    }

    public bool IsLuminosityIndex(string variableName)
    {
        var normalizedName = variableName.ToLowerInvariant();
        return (normalizedName.Contains("light") || normalizedName.Contains("luz") || normalizedName.Contains("luminosity")) &&
               (normalizedName.Contains("index") || normalizedName.Contains("indice") || normalizedName.Contains("quality") || normalizedName.Contains("calidad"));
    }

    public bool IsLuminosityClear(string variableName)
    {
        var normalizedName = variableName.ToLowerInvariant();
        return (normalizedName.Contains("light") || normalizedName.Contains("luz") || normalizedName.Contains("luminosity")) &&
               (normalizedName.Contains("clear") || normalizedName.Contains("cantidad") || normalizedName.Contains("quantity"));
    }

    private string GetOptimalRangeMessage(double value, Variable variable)
    {
        if (variable.OptimalMax.HasValue)
        {
            return $"Valor {value} fuera del rango óptimo [{variable.OptimalMin} - {variable.OptimalMax.Value}]";
        }
        else
        {
            return $"Valor {value} por debajo del mínimo óptimo {variable.OptimalMin}";
        }
    }

    public bool IsValueWithinOptimalRange(double value, Variable variable)
    {
        if (value < variable.OptimalMin)
            return false;

        if (variable.OptimalMax.HasValue && value > variable.OptimalMax.Value)
            return false;

        return true;
    }
}