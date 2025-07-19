using SensorService.Application.DTOs.Esp32Node;
using SensorService.Domain.ValueObjects;

namespace SensorService.Application.Interfaces.UseCases.Esp32OfflineWorker;

public interface ICheckEsp32OfflineStatusUseCase
{
    Task<CheckEsp32StatusResult> ExecuteAsync(
    OfflineThreshold threshold,
    DateTime? currentTime = null);
}
