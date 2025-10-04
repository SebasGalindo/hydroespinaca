using HydroEspinaca.Shared.Enums;

namespace ActuatorService.Application.DTOs;

/// <summary>
/// Represents a routine step with resolved physical actuator data
/// </summary>
public class ResolvedRoutineStepDto
{
    public string OutputVariableId { get; set; } = default!;
    public string OutputVariableName { get; set; } = default!;
    public string ActuatorId { get; set; } = default!;
    public string Esp32Id { get; set; } = default!;
    public string Pin { get; set; } = default!;
    public ActuatorMode Mode { get; set; }
    public string? Power { get; set; }
    public int? DutyCycle { get; set; }
    public int Duration { get; set; }
}
