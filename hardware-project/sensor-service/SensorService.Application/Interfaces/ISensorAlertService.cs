using SensorService.Application.DTOs;

namespace SensorService.Application.Interfaces;

public interface ISensorAlertService
{
    Task<List<SensorAlertDto>> GetBySensorIdAsync(string sensorId);
    Task AcknowledgeAsync(string alertId);
}