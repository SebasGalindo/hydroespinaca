using HydroEspinaca.Shared.DTOs.Mqtt;
using Microsoft.Extensions.Logging;
using SensorService.Application.Interfaces.UseCases.ProcessReadingBatch;
using SensorService.Domain.Entities;

namespace SensorService.Application.UseCases.ProcessReadingBatch;
public class GenerateInactiveSensorAlertsUseCase : IGenerateInactiveSensorAlertsUseCase
{
    private readonly ILogger<GenerateInactiveSensorAlertsUseCase> _logger;

    public GenerateInactiveSensorAlertsUseCase(
        ILogger<GenerateInactiveSensorAlertsUseCase> logger)
    {
        _logger = logger;
    }

    public Task<IEnumerable<SensorAlert>> ExecuteAsync(ReadingBatchDto dto)
    {
        // Disabled: Inactive sensor alerts are no longer generated
        // Only manual regulation type variables generate alerts based on optimal range
        _logger.LogDebug("InactiveSensorAlerts disabled - only manual variables generate alerts");
        return Task.FromResult(Enumerable.Empty<SensorAlert>());
    }
}