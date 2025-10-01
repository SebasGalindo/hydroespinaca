using HydroEspinaca.Shared.Enums;
using SensorService.Domain.Entities;
using SensorService.Domain.Interfaces;
using SensorService.Domain.ValueObjects;

namespace SensorService.Domain.Services;

public class CriticalReadingEvaluationService : ICriticalReadingEvaluationService
{
    private readonly IVariableRepository _variableRepository;

    public CriticalReadingEvaluationService(IVariableRepository variableRepository)
    {
        _variableRepository = variableRepository;
    }

    public async Task<CriticalAlertData> EvaluateCriticalReadingsAsync(string esp32Id, DateTime timestamp, IEnumerable<Reading> readings, IEnumerable<Sensor> sensors, CancellationToken cancellationToken = default)
    {
        var criticalReadings = new List<CriticalReadingAlert>();
        
        // Get only manual variables (critical variables that require human intervention)
        var manualVariables = await _variableRepository.GetByRegulationTypeAsync(RegulationType.Manual, cancellationToken);
        var variableLookup = manualVariables.ToDictionary(v => v.Id, v => v);
        
        if (!manualVariables.Any())
        {
            // No manual variables configured - return empty result
            return new CriticalAlertData
            {
                Esp32Id = esp32Id,
                Timestamp = timestamp,
                ManualReadings = Array.Empty<CriticalReadingAlert>()
            };
        }

        // Create a lookup for sensors by variable ID for efficient access
        var sensorsByVariableId = sensors
            .Where(s => s.Variables.Any(varId => variableLookup.ContainsKey(varId)))
            .SelectMany(s => s.Variables.Where(varId => variableLookup.ContainsKey(varId))
                .Select(varId => new { VariableId = varId, Sensor = s }))
            .GroupBy(sv => sv.VariableId)
            .ToDictionary(g => g.Key, g => g.Select(sv => sv.Sensor).ToList());

        // Group readings by sensor ID for efficient lookup
        var readingsBySensorId = readings
            .GroupBy(r => r.SensorId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Evaluate each manual variable
        foreach (var variable in manualVariables)
        {
            var thresholdDescription = GetThresholdDescription(variable);
            
            if (sensorsByVariableId.TryGetValue(variable.Id, out var sensorsForVariable))
            {
                // Find readings for this variable from any of its sensors
                var variableReadings = sensorsForVariable
                    .Where(sensor => readingsBySensorId.ContainsKey(sensor.Id))
                    .SelectMany(sensor => readingsBySensorId[sensor.Id])
                    .Where(reading => reading.VariableId == variable.Id)
                    .ToList();

                if (variableReadings.Any())
                {
                    // Use the most recent reading for this variable
                    var latestReading = variableReadings.OrderByDescending(r => r.Timestamp).First();
                    
                    var isAlert = IsValueOutOfRange(latestReading.Value, variable);
                    
                    criticalReadings.Add(new CriticalReadingAlert
                    {
                        Name = variable.Name,
                        Value = latestReading.Value,
                        Threshold = thresholdDescription,
                        IsAlert = isAlert
                    });
                }
                else
                {
                    // Variable has sensors but no readings - this is an alert
                    criticalReadings.Add(new CriticalReadingAlert
                    {
                        Name = variable.Name,
                        Value = double.NaN,
                        Threshold = thresholdDescription,
                        IsAlert = true
                    });
                }
            }
            else
            {
                // Variable has no associated sensors - this is a configuration issue
                criticalReadings.Add(new CriticalReadingAlert
                {
                    Name = variable.Name,
                    Value = double.NaN,
                    Threshold = "Sin sensores configurados",
                    IsAlert = true
                });
            }
        }
        
        return new CriticalAlertData
        {
            Esp32Id = esp32Id,
            Timestamp = timestamp,
            ManualReadings = criticalReadings.AsReadOnly()
        };
    }

    private static bool IsValueOutOfRange(double value, Variable variable)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
            return true;

        if (value < variable.OptimalMin)
            return true;

        if (variable.OptimalMax.HasValue && value > variable.OptimalMax.Value)
            return true;

        return false;
    }

    private static string GetThresholdDescription(Variable variable)
    {
        if (variable.OptimalMax.HasValue)
            return $"{variable.OptimalMin:F1} - {variable.OptimalMax.Value:F1} {variable.Unit}";
        else
            return $"≥ {variable.OptimalMin:F1} {variable.Unit}";
    }
}