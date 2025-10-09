namespace ActuatorService.Application.Configuration;

/// <summary>
/// Configuration for advanced actuator behavior rules
/// </summary>
public class AdvancedRulesConfiguration
{
    public const string SectionName = "AdvancedRules";

    public HumidifierRules Humidifier { get; set; } = new();
    public FullSpectrumLightRules FullSpectrumLight { get; set; } = new();
    public WaterHeaterRules WaterHeater { get; set; } = new();
}

public class HumidifierRules
{
    /// <summary>
    /// Maximum continuous ON time in minutes
    /// </summary>
    public int MaxContinuousOnMinutes { get; set; } = 10;

    /// <summary>
    /// Cooldown period after reaching max time (minutes)
    /// </summary>
    public int CooldownMinutes { get; set; } = 120; // 2 hours default
}

public class FullSpectrumLightRules
{
    /// <summary>
    /// Earliest hour when light can be turned ON (24h format)
    /// </summary>
    public int AllowedStartHour { get; set; } = 6;

    /// <summary>
    /// Latest hour when light must be turned OFF (24h format)
    /// </summary>
    public int AllowedEndHour { get; set; } = 18;

    /// <summary>
    /// Cooldown period after max time violation (minutes)
    /// </summary>
    public int CooldownMinutes { get; set; } = 10;

    /// <summary>
    /// Maximum continuous ON time in hours
    /// </summary>
    public int MaxContinuousOnHours { get; set; } = 12;
}

public class WaterHeaterRules
{
    /// <summary>
    /// Duration of automatic recirculation in seconds
    /// </summary>
    public int RecirculationDurationSeconds { get; set; } = 180; // 3 minutes

    /// <summary>
    /// Minimum interval between recirculations (minutes)
    /// </summary>
    public int MinIntervalBetweenRecirculationsMinutes { get; set; } = 15;
}
