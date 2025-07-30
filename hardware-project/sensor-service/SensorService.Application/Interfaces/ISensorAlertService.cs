using HydroEspinaca.Shared.DTOs.Alerts;
using SensorService.Application.DTOs.Alert;


namespace SensorService.Application.Interfaces;

public interface ISensorAlertService
{
    Task<List<SensorAlertDto>> GetBySensorIdAsync(string sensorId);
    Task AcknowledgeAsync(string id, SensorAlertUpdateDto dto);

}