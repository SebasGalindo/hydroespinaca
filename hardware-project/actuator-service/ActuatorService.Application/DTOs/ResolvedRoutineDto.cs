namespace ActuatorService.Application.DTOs;

/// <summary>
/// Represents a routine with all steps resolved to physical actuator data
/// </summary>
public class ResolvedRoutineDto
{
    public string RoutineId { get; set; } = default!;
    public string Esp32Id { get; set; } = default!;
    public List<ResolvedRoutineStepDto> ResolvedSteps { get; set; } = new();
}
