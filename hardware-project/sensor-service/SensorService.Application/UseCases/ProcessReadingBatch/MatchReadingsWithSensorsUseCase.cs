using HydroEspinaca.Shared.DTOs.Mqtt;
using Microsoft.Extensions.Logging;
using SensorService.Application.Interfaces.UseCases.ProcessReadingBatch;
using SensorService.Domain.Entities;
using SensorService.Domain.Interfaces;

namespace SensorService.Application.UseCases.ProcessReadingBatch;

/// <summary>
/// Caso de uso para emparejar lecturas entrantes con los sensores registrados en el sistema.
/// Busca coincidencias por identificador físico del sensor y código de variable,
/// transformando las lecturas MQTT en entidades de dominio Reading.
/// </summary>
public class MatchReadingsWithSensorsUseCase : IMatchReadingsWithSensorsUseCase
{
    private readonly ISensorRepository _sensorRepository;
    private readonly ILogger<MatchReadingsWithSensorsUseCase> _logger;

    public MatchReadingsWithSensorsUseCase(ISensorRepository sensorRepository, ILogger<MatchReadingsWithSensorsUseCase> logger)
    {
        _sensorRepository = sensorRepository;
        _logger = logger;
    }

    /// <summary>
    /// Empareja cada lectura del lote con un sensor registrado usando PhysicalId y VariableCode.
    /// Las lecturas sin sensor coincidente se registran como advertencia y se descartan.
    /// </summary>
    /// <param name="dto">DTO del lote de lecturas recibido del ESP32.</param>
    /// <returns>Colección de lecturas emparejadas como entidades de dominio Reading.</returns>
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
            _logger.LogDebug("📈 Processing reading: PhysicalId='{PhysicalId}', VariableCode='{VariableCode}', Value={Value}",
                reading.PhysicalId, reading.VariableCode, reading.Value);

            var matchedSensor = allSensors.FirstOrDefault(s =>
                s.PhysicalId == reading.PhysicalId &&
                s.Variables.Contains(reading.VariableCode));

            if (matchedSensor != null)
            {
                _logger.LogInformation("✅ Matched reading: PhysicalId='{PhysicalId}' -> SensorCode='{SensorCode}'",
                    reading.PhysicalId, matchedSensor.Code);

                matchedReadings.Add(new Reading
                {
                    SensorCode = matchedSensor.Code,
                    VariableCode = reading.VariableCode,
                    Value = reading.Value,
                    Timestamp = dto.Timestamp.UtcDateTime
                });
            }
            else
            {
                _logger.LogWarning("❌ No matching sensor found for PhysicalId='{PhysicalId}', VariableCode='{VariableCode}'",
                    reading.PhysicalId, reading.VariableCode);
            }
        }

        _logger.LogInformation("🎯 Total matched readings: {MatchedCount} out of {TotalCount}", 
            matchedReadings.Count, dto.Readings.Count);

        return matchedReadings;
    }
}