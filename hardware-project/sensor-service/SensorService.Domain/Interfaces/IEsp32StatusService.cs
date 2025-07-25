using SensorService.Domain.Entities;
using SensorService.Domain.ValueObjects;

namespace SensorService.Domain.Interfaces;

public interface IEsp32StatusService
{
    Task<IEnumerable<Esp32StatusRecord>> GetAllEsp32StatusesAsync(
        DateTime currentTime,
        OfflineThreshold threshold);

    SensorAlert CreateOfflineAlert(
        Esp32StatusRecord status,
        string sensorId,
        DateTime timestamp);
}
