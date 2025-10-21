using SensorService.Domain.ValueObjects;

namespace SensorService.Domain.Interfaces;

public interface ICriticalAlertNotificationService
{
    Task SendCriticalAlertAsync(CriticalAlertData alertData, CancellationToken cancellationToken = default);
    Task<bool> ShouldSendAlertAsync(IEnumerable<string> variableCodes, CancellationToken cancellationToken = default);
    Task MarkAlertAsSentAsync(IEnumerable<string> variableCodes, CancellationToken cancellationToken = default);
}