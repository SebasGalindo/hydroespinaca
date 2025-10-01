using SensorService.Domain.ValueObjects;

namespace SensorService.Domain.Interfaces;

public interface ICriticalAlertNotificationService
{
    Task SendCriticalAlertAsync(CriticalAlertData alertData, CancellationToken cancellationToken = default);
    Task<bool> ShouldSendAlertAsync(string esp32Id, IEnumerable<string> alertVariables, CancellationToken cancellationToken = default);
    Task MarkAlertAsSentAsync(string esp32Id, IEnumerable<string> alertVariables, CancellationToken cancellationToken = default);
    Task MarkAlertAsResolvedAsync(string esp32Id, IEnumerable<string> alertVariables, CancellationToken cancellationToken = default);
}