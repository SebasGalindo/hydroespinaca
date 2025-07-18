using SensorService.Application.DTOs.Mqtt;
using SensorService.Application.UseCases.ProcessReadingBatch;

namespace SensorService.Application.Interfaces.UseCases.ProcessReadingBatch;
public interface IProcessReadingBatchUseCase
{
    Task<Result<ProcessReadingBatchOutput>> ExecuteAsync(ReadingBatchDto dto, CancellationToken cancellationToken = default);
}