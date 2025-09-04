namespace SensorService.Application.Interfaces.UseCases.Esp32Status;

public interface IHandleEsp32StatusUseCase
{
    Task HandleOnlineAsync(string esp32Id, DateTime timestamp);
    Task HandleOfflineAsync(string esp32Id, DateTime timestamp);
}