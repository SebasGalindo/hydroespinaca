using HydroEspinaca.Shared.Enums;
using HydroEspinaca.Shared.Abstractions;

namespace ActuatorService.Domain.Entities;

public class ActuatorCommand : IEntity
{
    public string Id { get; set; } = default!;
    public string ActuatorId { get; set; } = default!;
    public string Esp32Id { get; set; } = default!;
    public string Action { get; set; } = default!;         // e.g., "on", "off", or a PWM string like "pwm:120"
    public int? DurationMs { get; set; }                   // optional for "on/off"
    public TriggerType Trigger { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public bool Acknowledged { get; set; } = false;

    public string? RoutineId { get; set; }
    public int? RoutineStepOrder { get; set; }

    public string? UserId { get; set; } // null if automated

    public CommandMetadata? Metadata { get; set; }
}

public class CommandMetadata
{
    public string Source { get; set; } = default!;
    public string? FuzzyRule { get; set; }
    public Dictionary<string, double>? Inputs { get; set; }
}