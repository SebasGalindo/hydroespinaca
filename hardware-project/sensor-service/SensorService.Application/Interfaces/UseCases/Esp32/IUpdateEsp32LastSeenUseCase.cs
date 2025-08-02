namespace SensorService.Application.Interfaces.UseCases.Esp32;
public interface IUpdateEsp32LastSeenUseCase
{
    Task ExecuteAsync(string esp32Id, DateTime timestamp);
}