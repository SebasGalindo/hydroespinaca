using HydroEspinaca.Shared.DTOs.Alerts;
using SensorService.Application.DTOs.Alert;

namespace SensorService.Application.Interfaces;

public interface IEsp32AlertService
{
    Task<List<Esp32AlertDto>> GetByEsp32IdAsync(string esp32Id);
    Task AcknowledgeAsync(string id, Esp32AlertUpdateDto dto);
}
