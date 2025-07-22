using HydroEspinaca.Shared.Enums;

namespace ActuatorService.Application.DTOs;

public class CreateCommandDto
{
    public string ActuatorId { get; set; } = default!;
    public string Esp32Id { get; set; } = default!;
    public string Action { get; set; } = default!;
    public int? DurationMs { get; set; }
    public TriggerType Trigger { get; set; }
    public string? RoutineId { get; set; }
    public int? RoutineStepOrder { get; set; }
    
    public CommandMetadataDto? Metadata { get; set; }
}
public class CommandMetadataDto
{
    public string Source { get; set; } = default!;
    public string? FuzzyRule { get; set; }
    public Dictionary<string, double>? Inputs { get; set; }
}   
