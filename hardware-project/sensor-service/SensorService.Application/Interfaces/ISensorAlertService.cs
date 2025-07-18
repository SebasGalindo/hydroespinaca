using SensorService.Application.DTOs.Alert;

namespace SensorService.Application.Interfaces;

public interface ISensorAlertService
{
    Task<List<SensorAlertDto>> GetBySensorIdAsync(string sensorId);
    Task AcknowledgeAsync(SensorAlertUpdateDto dto);

}