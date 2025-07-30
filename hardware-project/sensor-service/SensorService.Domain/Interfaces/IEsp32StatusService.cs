using SensorService.Domain.Entities;
using SensorService.Domain.ValueObjects;

namespace SensorService.Domain.Interfaces;

public interface IEsp32StatusService
{
    Task<IEnumerable<Esp32StatusRecord>> GetAllEsp32StatusesAsync(
        DateTime currentTime,
        OfflineThreshold threshold);
    Task UpsertOfflineAlertAsync(
      Esp32StatusRecord status,
      DateTime timestamp);
    Task AcknowledgeOfflineAlertAsync(string esp32Id);

}
