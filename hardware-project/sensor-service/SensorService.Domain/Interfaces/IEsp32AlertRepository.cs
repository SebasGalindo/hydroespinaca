using SensorService.Domain.Entities;

namespace SensorService.Domain.Interfaces;

public interface IEsp32AlertRepository
{
    Task<Esp32Alert?> GetActiveByEsp32IdAsync(string esp32Id);
    Task CreateAsync(Esp32Alert alert);
    Task UpdateAsync(Esp32Alert alert);
    Task<Esp32Alert?> GetByIdAsync(string id);
    Task<List<Esp32Alert>> GetByEsp32IdAsync(string esp32Id);
    Task<int> DeleteOlderThanAsync(DateTime cutoffDate);
    Task<List<Esp32Alert>> GetUnsentEmailAlertsByEsp32IdsAsync(IEnumerable<string> esp32Ids, CancellationToken cancellationToken = default);
    Task MarkEmailAsSentAsync(string alertId, DateTime sentAt, CancellationToken cancellationToken = default);
}
