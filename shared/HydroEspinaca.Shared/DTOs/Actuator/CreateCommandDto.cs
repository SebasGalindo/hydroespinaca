namespace HydroEspinaca.Shared.DTOs.Actuator;
public class CreateCommandDto
{
    public string ActuatorId { get; set; } = default!;
    public string Esp32Id { get; set; } = default!;
    public string Action { get; set; } = default!;
    public int? DurationMs { get; set; }
    public string Trigger { get; set; }
    public string? RoutineId { get; set; }
    public int? RoutineStepOrder { get; set; }

    public CommandMetadataDto? Metadata { get; set; }
}
public class CommandMetadataDto
{
    public required string Source { get; set; }
    public string? FuzzyRule { get; set; }
    public Dictionary<string, double>? Inputs { get; set; }
}

