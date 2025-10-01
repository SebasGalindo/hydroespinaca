using SensorService.Domain.Entities;

namespace SensorService.Application.Interfaces;

public interface ICriticalAlertApplicationService
{
    Task ProcessCriticalAlertsAsync(string esp32Id, DateTime timestamp, IEnumerable<Reading> readings, CancellationToken cancellationToken = default);
}