using SensorService.Domain.Entities;

namespace SensorService.Domain.Interfaces;
public interface IAlertCalculationService
{
    SensorAlert? CalculateOutOfRangeAlert(Reading reading, Variable variable, DateTime timestamp);
    bool IsValueWithinOptimalRange(double value, Variable variable);
}