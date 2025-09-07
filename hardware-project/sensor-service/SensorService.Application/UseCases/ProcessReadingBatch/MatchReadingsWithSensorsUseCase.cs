using HydroEspinaca.Shared.DTOs.Mqtt;
using SensorService.Application.Interfaces.UseCases.ProcessReadingBatch;
using SensorService.Domain.Entities;
using SensorService.Domain.Interfaces;

namespace SensorService.Application.UseCases.ProcessReadingBatch;
public class MatchReadingsWithSensorsUseCase : IMatchReadingsWithSensorsUseCase
{
    private readonly ISensorRepository _sensorRepository;

    public MatchReadingsWithSensorsUseCase(ISensorRepository sensorRepository)
    {
        _sensorRepository = sensorRepository;
    }

    public async Task<IEnumerable<Reading>> ExecuteAsync(ReadingBatchDto dto)
    {
        var allSensors = await _sensorRepository.GetAllAsync();
        var matchedReadings = new List<Reading>();

        foreach (var reading in dto.Readings)
        {
            var matchedSensor = allSensors.FirstOrDefault(s =>
                s.PhysicalId == reading.PhysicalId &&
                s.Variables.Contains(reading.VariableId));

            if (matchedSensor != null)
            {
                matchedReadings.Add(new Reading
                {
                    SensorId = matchedSensor.Id!,
                    VariableId = reading.VariableId,
                    Value = reading.Value,
                    Timestamp = dto.Timestamp.UtcDateTime
                });
            }
        }

        return matchedReadings;
    }
}