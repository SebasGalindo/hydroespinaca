using SensorService.Application.DTOs.Aggregate;

namespace SensorService.Application.Interfaces.UseCases.AggregateWorker;

public interface IProcessAggregatesUseCase
{
    Task<ProcessAggregatesResult> ExecuteAsync(DateTime referenceTime);
}
