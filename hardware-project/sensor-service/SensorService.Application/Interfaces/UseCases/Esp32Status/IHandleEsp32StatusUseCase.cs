using SensorService.Application.DTOs.Esp32Status;

namespace SensorService.Application.Interfaces.UseCases.Esp32Status;

public interface IHandleEsp32StatusUseCase
{
    Task HandleOnlineAsync(string esp32Id, DateTime timestamp, long? freeHeap = null, long? uptime = null);
    Task HandleOfflineAsync(string esp32Id, DateTime timestamp);
    Task HandleStatusPayloadAsync(Esp32StatusPayloadDto payload);
}