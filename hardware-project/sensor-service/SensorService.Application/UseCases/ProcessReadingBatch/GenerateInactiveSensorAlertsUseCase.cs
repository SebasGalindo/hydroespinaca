using HydroEspinaca.Shared.Constants;
using HydroEspinaca.Shared.DTOs.Mqtt;
using HydroEspinaca.Shared.Enums;
using SensorService.Application.Interfaces.UseCases.ProcessReadingBatch;
using SensorService.Domain.Entities;
using SensorService.Domain.Interfaces;

namespace SensorService.Application.UseCases.ProcessReadingBatch;
public class GenerateInactiveSensorAlertsUseCase : IGenerateInactiveSensorAlertsUseCase
{
    private readonly ISensorRepository _sensorRepository;

    public GenerateInactiveSensorAlertsUseCase(ISensorRepository sensorRepository)
    {
        _sensorRepository = sensorRepository;
    }

    public async Task<IEnumerable<SensorAlert>> ExecuteAsync(ReadingBatchDto dto)
    {
        var allSensors = await _sensorRepository.GetAllAsync();
        var alerts = new List<SensorAlert>();

        var expectedSensors = allSensors
             .Where(s => s.Esp32Id == dto.Esp32Id && s.Status == SensorStatus.Active)
            .SelectMany(s => s.Variables.Select(v => new { s.Id, s.PhysicalId, VariableId = v }))
            .ToList();

        var receivedKeys = dto.Readings
            .Select(r => $"{r.PhysicalId}-{r.VariableId}")
            .ToHashSet();

        foreach (var expected in expectedSensors)
        {
            var key = $"{expected.PhysicalId}-{expected.VariableId}";
            if (!receivedKeys.Contains(key))
            {
                alerts.Add(new SensorAlert
                {
                    SensorId = expected.Id,
                    Type = AlertType.InactiveSensor,
                    Timestamp = dto.Timestamp,
                    Severity = AlertSeverity.Critical,
                    Message = $"No se recibió lectura esperada de {expected.PhysicalId} - {expected.VariableId}",
                    Acknowledged = false
                });
            }
        }

        return alerts;
    }
}