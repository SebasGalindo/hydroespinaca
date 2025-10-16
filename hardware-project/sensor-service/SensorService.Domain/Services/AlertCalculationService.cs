using SensorService.Domain.Entities;
using SensorService.Domain.Interfaces;

namespace SensorService.Domain.Services;
public class AlertCalculationService : IAlertCalculationService
{
    public SensorAlert? CalculateOutOfRangeAlert(Reading reading, Variable variable, DateTime timestamp)
    {
        var value = reading.Value;

        // Check if value is below OptimalMin
        if (value < variable.OptimalMin)
        {
            return new SensorAlert
            {
                VariableCode = reading.VariableCode,
                Value = value,
                Timestamp = timestamp,
                Message = GetOptimalRangeMessage(value, variable),
                LastSeen = timestamp,
                Acknowledged = false
            };
        }

        // Check if value is above OptimalMax (only if OptimalMax is defined)
        if (variable.OptimalMax.HasValue && value > variable.OptimalMax.Value)
        {
            return new SensorAlert
            {
                VariableCode = reading.VariableCode,
                Value = value,
                Timestamp = timestamp,
                Message = GetOptimalRangeMessage(value, variable),
                LastSeen = timestamp,
                Acknowledged = false
            };
        }

        return null;
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