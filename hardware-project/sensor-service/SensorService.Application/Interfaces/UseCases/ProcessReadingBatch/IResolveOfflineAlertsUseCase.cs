namespace SensorService.Application.Interfaces.UseCases.ProcessReadingBatch;
public interface IResolveOfflineAlertsUseCase
{
    Task ExecuteAsync(string esp32Id);
}