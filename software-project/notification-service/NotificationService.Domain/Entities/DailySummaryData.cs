namespace NotificationService.Domain.Entities;

/// <summary>
/// Aggregated data for the daily summary notification.
/// Contains sensor averages, actuator runtime, fuzzy evaluations and weather forecast.
/// </summary>
public class DailySummaryData
{
    public DateTime Date { get; set; }
    public string UserId { get; set; } = string.Empty;

    // Sensor data
    public List<SensorVariableSummary> SensorSummaries { get; set; } = [];

    // Actuator data
    public List<ActuatorRuntimeSummary> ActuatorSummaries { get; set; } = [];

    // Fuzzy data
    public FuzzyEvaluationSummary? FuzzyEvaluation { get; set; }

    // Weather data
    public WeatherForecastSummary? WeatherForecast { get; set; }

    // Config flags (for template rendering)
    public bool IncludeSensorAverages { get; set; }
    public bool IncludeActuatorRuntime { get; set; }
    public bool IncludeFuzzyRules { get; set; }
    public bool IncludeWeatherForecast { get; set; }
}

public class SensorVariableSummary
{
    public string VariableCode { get; set; } = string.Empty;
    public string VariableName { get; set; } = string.Empty;
    public double Min { get; set; }
    public double Max { get; set; }
    public double Avg { get; set; }
    public int Count { get; set; }
}

public class ActuatorRuntimeSummary
{
    public string ActuatorCode { get; set; } = string.Empty;
    public double TotalDurationMinutes { get; set; }
    public double Percentage { get; set; }
    public int ActivationCount { get; set; }
}

public class FuzzyEvaluationSummary
{
    public int EvaluationCount { get; set; }
    public string? SystemName { get; set; }
    public List<TopRuleSummary> TopRules { get; set; } = [];
}

public class TopRuleSummary
{
    public string RuleId { get; set; } = string.Empty;
    public string? RuleName { get; set; }
    public int ActivationCount { get; set; }
    public double AvgFiringStrength { get; set; }
}

public class WeatherForecastSummary
{
    public double TempMin { get; set; }
    public double TempMax { get; set; }
    public double Pop { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Summary { get; set; }
}
