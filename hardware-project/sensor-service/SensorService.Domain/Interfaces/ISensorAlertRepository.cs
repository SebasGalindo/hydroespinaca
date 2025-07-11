using SensorService.Domain.Entities;

namespace SensorService.Domain.Interfaces;
public interface ISensorAlertRepository
{
    Task CreateAsync(SensorAlert alert);
    Task<List<SensorAlert>> GetBySensorIdAsync(string sensorId);
    Task AcknowledgeAsync(string alertId);
}
