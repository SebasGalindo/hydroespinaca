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

    public async Task<IEnumerable<SensorAlert>> ExecuteAsync(ReadingBatchDto dto)
    {
        var allSensors = await _sensorRepository.GetAllAsync();
        var alerts = new List<SensorAlert>();

        var expectedSensors = allSensors
             .Where(s => s.Esp32Id == dto.Esp32Id && s.Status == SensorStatus.Active)
            .SelectMany(s => s.Variables.Select(v => new { s.Code, s.PhysicalId, VariableCode = v, s.AllowMissing }))
            .ToList();

        var receivedKeys = dto.Readings
            .Select(r => $"{r.PhysicalId}-{r.VariableCode}")
            .ToHashSet();

        foreach (var expected in expectedSensors)
        {
            var key = $"{expected.PhysicalId}-{expected.VariableCode}";
            if (!receivedKeys.Contains(key))
            {
                // Check if sensor allows missing readings
                if (expected.AllowMissing)
                {
                    _logger.LogInformation("⚠️ Sensor {SensorCode} omitido en validación de inactividad (allowMissing = true)", expected.Code);
                    continue; // Don't generate InactiveSensor alert for sensors that allow missing readings
                }

                // Get the variable to check if it's a Luminosity Index
                var variable = await _variableRepository.GetByCodeAsync(expected.VariableCode);
                
                // Skip inactive sensor alerts for Luminosity Index variables
                // because firmware filters them out when C < 3000
                if (variable != null && _alertCalculationService.IsLuminosityIndex(variable.Name))
                {
                    continue; // Don't generate InactiveSensor alert for Luminosity Index
                }

                alerts.Add(new SensorAlert
                {
                    SensorCode = expected.Code,
                    VariableCode = expected.VariableCode,
                    Type = AlertType.InactiveSensor,
                    Timestamp = dto.Timestamp.DateTime,
                    Severity = AlertSeverity.Critical,
                    Message = $"No se recibió lectura esperada de {expected.PhysicalId} - {expected.VariableCode}",
                    Acknowledged = false
                });
            }
        }

        return alerts;
    }
}