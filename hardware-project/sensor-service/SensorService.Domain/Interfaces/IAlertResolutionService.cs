using SensorService.Domain.Entities;

namespace SensorService.Domain.Interfaces;

public interface IAlertResolutionService
{
    Task ResolveOutOfRangeAlertsAsync(Reading reading, Variable variable, DateTime timestamp);
}