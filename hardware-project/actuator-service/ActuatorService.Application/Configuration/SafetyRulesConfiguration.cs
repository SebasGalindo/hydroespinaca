namespace ActuatorService.Application.Configuration;

/// <summary>
/// Configuration for actuator safety rules
/// </summary>
public class SafetyRulesConfiguration
{
    public const string SectionName = "SafetyRules";

    /// <summary>
    /// How often to check for safety violations (in seconds)
    /// </summary>
    public int CheckIntervalSeconds { get; set; } = 60;

    /// <summary>
    /// Safety limits for specific actuators
    /// </summary>
    public List<ActuatorSafetyLimit> Limits { get; set; } = new();
}

/// <summary>
/// Safety limit for a specific actuator type or name
/// </summary>
public class ActuatorSafetyLimit
{
    /// <summary>
    /// Actuator physical ID (e.g., "HUMIDIFIER-001")
    /// </summary>
    public string? PhysicalId { get; set; }

    /// <summary>
    /// Actuator code (e.g., "Humidificador")
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    /// Maximum time in seconds the actuator can stay ON
    /// </summary>
    public int MaxOnTimeSeconds { get; set; }

    /// <summary>
    /// Description of the safety rule
    /// </summary>
    public string Description { get; set; } = default!;
}
