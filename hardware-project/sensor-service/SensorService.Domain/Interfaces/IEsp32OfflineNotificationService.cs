using SensorService.Domain.Entities;

namespace SensorService.Domain.Interfaces;

public interface IEsp32OfflineNotificationService
{
    Task SendOfflineAlertAsync(Esp32Alert alert, CancellationToken cancellationToken = default);
    Task<bool> ShouldSendAlertAsync(string esp32Id, CancellationToken cancellationToken = default);
    Task MarkAlertAsSentAsync(string alertId, CancellationToken cancellationToken = default);
}
