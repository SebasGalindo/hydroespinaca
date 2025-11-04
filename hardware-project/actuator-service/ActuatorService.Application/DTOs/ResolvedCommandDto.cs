using HydroEspinaca.Shared.Enums;

namespace ActuatorService.Application.DTOs;

/// <summary>
/// Represents an actuator command with resolved physical actuator data
/// Simplified version without ControlOutput dependency
/// </summary>
public class ResolvedCommandDto
{
    /// <summary>
    /// Optional pre-generated CommandId. If provided, CommandExecutionService will use this ID
    /// instead of generating a new one. This ensures consistency between DB records and MQTT messages.
    /// </summary>
    public string? CommandId { get; set; }

    public string ActuatorCode { get; set; } = default!;
    public string ActuatorId { get; set; } = default!;
    public string Esp32Id { get; set; } = default!;
    public string Pin { get; set; } = default!;
    public ActuatorMode Mode { get; set; }
    public string? Power { get; set; }
    public double? DutyCycle { get; set; }
    public double Duration { get; set; }
}
