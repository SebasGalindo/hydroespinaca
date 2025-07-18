using SensorService.Domain.Entities;

namespace SensorService.Application.Interfaces.UseCases.ProcessReadingBatch;
public interface IGenerateAlertsUseCase
{
    Task<IEnumerable<SensorAlert>> ExecuteAsync(IEnumerable<Reading> readings, DateTime timestamp);
}