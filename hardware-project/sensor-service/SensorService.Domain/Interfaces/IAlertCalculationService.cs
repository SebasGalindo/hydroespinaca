using SensorService.Domain.Entities;

namespace SensorService.Domain.Interfaces;
public interface IAlertCalculationService
{
    SensorAlert? CalculateOutOfRangeAlert(Reading reading, Variable variable, DateTime timestamp);
    SensorAlert? CalculateAnomalyAlert(Reading reading, Aggregate latestAggregate, DateTime timestamp);
    SensorAlert? CalculateLuminosityAlert(Reading reading, Variable variable, DateTime timestamp);
    bool IsLuminosityVariable(string variableName);
    bool IsLuminosityIndex(string variableName);
    bool IsLuminosityClear(string variableName);
    bool IsValueWithinOptimalRange(double value, Variable variable);
}