using HydroEspinaca.Shared.DTOs.Mqtt;
using SensorService.Domain.Entities;

namespace SensorService.Application.Interfaces.UseCases.ProcessReadingBatch;
public interface IGenerateInactiveSensorAlertsUseCase
{
    Task<IEnumerable<SensorAlert>> ExecuteAsync(ReadingBatchDto dto);
}