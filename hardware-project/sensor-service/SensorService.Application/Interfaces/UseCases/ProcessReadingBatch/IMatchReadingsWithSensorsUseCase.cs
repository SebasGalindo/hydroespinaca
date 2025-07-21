using HydroEspinaca.Shared.DTOs.Mqtt;
using SensorService.Domain.Entities;

namespace SensorService.Application.Interfaces.UseCases.ProcessReadingBatch;
public interface IMatchReadingsWithSensorsUseCase
{
    Task<IEnumerable<Reading>> ExecuteAsync(ReadingBatchDto dto);
}