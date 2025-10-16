using HydroEspinaca.Shared.Constants;
using HydroEspinaca.Shared.DTOs.Mqtt;
using HydroEspinaca.Shared.Enums;
using Microsoft.Extensions.Logging;
using SensorService.Application.Interfaces.UseCases.ProcessReadingBatch;
using SensorService.Domain.Entities;
using SensorService.Domain.Interfaces;

namespace SensorService.Application.UseCases.ProcessReadingBatch;
public class GenerateInactiveSensorAlertsUseCase : IGenerateInactiveSensorAlertsUseCase
{
    private readonly ISensorRepository _sensorRepository;
    private readonly IVariableRepository _variableRepository;
    private readonly IAlertCalculationService _alertCalculationService;
    private readonly ILogger<GenerateInactiveSensorAlertsUseCase> _logger;

    public GenerateInactiveSensorAlertsUseCase(
        ISensorRepository sensorRepository,
        IVariableRepository variableRepository,
        IAlertCalculationService alertCalculationService,
        ILogger<GenerateInactiveSensorAlertsUseCase> logger)
    {
        _sensorRepository = sensorRepository;
        _variableRepository = variableRepository;
        _alertCalculationService = alertCalculationService;
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