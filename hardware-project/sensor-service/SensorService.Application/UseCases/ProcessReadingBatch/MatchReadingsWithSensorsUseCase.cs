using HydroEspinaca.Shared.DTOs.Mqtt;
using Microsoft.Extensions.Logging;
using SensorService.Application.Interfaces.UseCases.ProcessReadingBatch;
using SensorService.Domain.Entities;
using SensorService.Domain.Interfaces;

namespace SensorService.Application.UseCases.ProcessReadingBatch;
public class MatchReadingsWithSensorsUseCase : IMatchReadingsWithSensorsUseCase
{
    private readonly ISensorRepository _sensorRepository;
    private readonly ILogger<MatchReadingsWithSensorsUseCase> _logger;

    public MatchReadingsWithSensorsUseCase(ISensorRepository sensorRepository, ILogger<MatchReadingsWithSensorsUseCase> logger)
    {
        _sensorRepository = sensorRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<Reading>> ExecuteAsync(ReadingBatchDto dto)
    {
        var allSensors = await _sensorRepository.GetAllAsync();
        var matchedReadings = new List<Reading>();

        _logger.LogInformation("🔍 Sensors found in database: {SensorCount}", allSensors.Count);
        _logger.LogInformation("📊 Incoming readings to process: {ReadingCount}", dto.Readings.Count);

        foreach (var sensor in allSensors)
        {
            _logger.LogDebug("📋 Sensor: PhysicalId='{PhysicalId}', Variables=[{Variables}]", 
                sensor.PhysicalId, string.Join(", ", sensor.Variables));
        }

        foreach (var reading in dto.Readings)
        {
            _logger.LogDebug("📈 Processing reading: PhysicalId='{PhysicalId}', VariableId='{VariableId}', Value={Value}", 
                reading.PhysicalId, reading.VariableId, reading.Value);

            var matchedSensor = allSensors.FirstOrDefault(s =>
                s.PhysicalId == reading.PhysicalId &&
                s.Variables.Contains(reading.VariableId));

            if (matchedSensor != null)
            {
                _logger.LogInformation("✅ Matched reading: PhysicalId='{PhysicalId}' -> SensorId='{SensorId}'", 
                    reading.PhysicalId, matchedSensor.Id);

                matchedReadings.Add(new Reading
                {
                    SensorId = matchedSensor.Id!,
                    VariableId = reading.VariableId,
                    Value = reading.Value,
                    Timestamp = dto.Timestamp.UtcDateTime
                });
            }
            else
            {
                _logger.LogWarning("❌ No matching sensor found for PhysicalId='{PhysicalId}', VariableId='{VariableId}'", 
                    reading.PhysicalId, reading.VariableId);
            }
        }

        _logger.LogInformation("🎯 Total matched readings: {MatchedCount} out of {TotalCount}", 
            matchedReadings.Count, dto.Readings.Count);

        return matchedReadings;
    }
}