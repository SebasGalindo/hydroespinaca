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
        var contextualReadings = new List<ContextualReading>();

        // Get only manual variables (critical variables that require human intervention)
        var manualVariables = await _variableRepository.GetByRegulationTypeAsync(RegulationType.Manual, cancellationToken);
        var variableLookup = manualVariables.ToDictionary(v => v.Code, v => v);

        // Get automatic variables for contextual information
        var automaticVariables = await _variableRepository.GetByRegulationTypeAsync(RegulationType.Automatic, cancellationToken);
        var automaticVariableLookup = automaticVariables.ToDictionary(v => v.Code, v => v);

        if (!manualVariables.Any())
        {
            // No manual variables configured - return empty result
            return new CriticalAlertData
            {
                Esp32Id = esp32Id,
                Timestamp = timestamp,
                ManualReadings = Array.Empty<CriticalReadingAlert>(),
                ContextualReadings = Array.Empty<ContextualReading>()
            };
        }

        // Create a lookup for sensors by variable Code for efficient access
        var sensorsByVariableCode = sensors
            .Where(s => s.Variables.Any(varCode => variableLookup.ContainsKey(varCode)))
            .SelectMany(s => s.Variables.Where(varCode => variableLookup.ContainsKey(varCode))
                .Select(varCode => new { VariableCode = varCode, Sensor = s }))
            .GroupBy(sv => sv.VariableCode)
            .ToDictionary(g => g.Key, g => g.Select(sv => sv.Sensor).ToList());

        // Group readings by sensor code for efficient lookup
        var readingsBySensorCode = readings
            .GroupBy(r => r.SensorCode)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Evaluate each manual variable
        foreach (var variable in manualVariables)
        {
            var thresholdDescription = GetThresholdDescription(variable);

            if (sensorsByVariableCode.TryGetValue(variable.Code, out var sensorsForVariable))
            {
                // Find readings for this variable from any of its sensors
                var variableReadings = sensorsForVariable
                    .Where(sensor => readingsBySensorCode.ContainsKey(sensor.Code))
                    .SelectMany(sensor => readingsBySensorCode[sensor.Code])
                    .Where(reading => reading.VariableCode == variable.Code)
                    .ToList();

                if (variableReadings.Any())
                {
                    // Use the most recent reading for this variable
                    var latestReading = variableReadings.OrderByDescending(r => r.Timestamp).First();
                    
                    var isAlert = IsValueOutOfRange(latestReading.Value, variable);
                    
                    criticalReadings.Add(new CriticalReadingAlert
                    {
                        Code = variable.Code,
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
                        Code = variable.Code,
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
                    Code = variable.Code,
                    Name = variable.Name,
                    Value = double.NaN,
                    Threshold = "Sin sensores configurados",
                    IsAlert = true
                });
            }
        }

        // ✅ Collect contextual readings from automatic sensors (temperature, humidity)
        // These provide context for why the alert was triggered, but are NOT alerts themselves
        var automaticSensorsByVariableCode = sensors
            .Where(s => s.Variables.Any(varCode => automaticVariableLookup.ContainsKey(varCode)))
            .SelectMany(s => s.Variables.Where(varCode => automaticVariableLookup.ContainsKey(varCode))
                .Select(varCode => new { VariableCode = varCode, Sensor = s }))
            .GroupBy(sv => sv.VariableCode)
            .ToDictionary(g => g.Key, g => g.Select(sv => sv.Sensor).ToList());

        foreach (var automaticVariable in automaticVariables)
        {
            if (automaticSensorsByVariableCode.TryGetValue(automaticVariable.Code, out var sensorsForVariable))
            {
                // Find readings for this automatic variable
                var variableReadings = sensorsForVariable
                    .Where(sensor => readingsBySensorCode.ContainsKey(sensor.Code))
                    .SelectMany(sensor => readingsBySensorCode[sensor.Code])
                    .Where(reading => reading.VariableCode == automaticVariable.Code)
                    .ToList();

                if (variableReadings.Any())
                {
                    // Use the most recent reading for context
                    var latestReading = variableReadings.OrderByDescending(r => r.Timestamp).First();

                    if (!double.IsNaN(latestReading.Value) && !double.IsInfinity(latestReading.Value))
                    {
                        contextualReadings.Add(new ContextualReading
                        {
                            Name = automaticVariable.Name,
                            Value = latestReading.Value,
                            Unit = automaticVariable.Unit
                        });
                    }
                }
            }
        }

        return new CriticalAlertData
        {
            Esp32Id = esp32Id,
            Timestamp = timestamp,
            ManualReadings = criticalReadings.AsReadOnly(),
            ContextualReadings = contextualReadings.AsReadOnly()
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