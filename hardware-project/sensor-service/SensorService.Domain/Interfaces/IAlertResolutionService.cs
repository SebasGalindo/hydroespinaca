using SensorService.Domain.Entities;

namespace SensorService.Domain.Interfaces;

public interface IAlertResolutionService
{
    Task ResolveOutOfRangeAlertsAsync(Reading reading, Variable variable, DateTime timestamp);
    Task ResolveInactiveSensorAlertsAsync(string sensorId, string variableId, DateTime timestamp);
    Task ResolveAlertAsync(SensorAlert alert, string resolutionReason, DateTime timestamp);
}