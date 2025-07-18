using SensorService.Domain.Entities;

namespace SensorService.Domain.Interfaces;
public interface IAlertCalculationService
{
    SensorAlert? CalculateOutOfRangeAlert(Reading reading, Variable variable, DateTime timestamp);
    SensorAlert? CalculateAnomalyAlert(Reading reading, Aggregate latestAggregate, DateTime timestamp);
}