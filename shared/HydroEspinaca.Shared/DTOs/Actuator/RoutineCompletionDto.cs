namespace HydroEspinaca.Shared.DTOs.Actuator;

public class RoutineCompletionDto
{
    public string RoutineId { get; set; } = default!;
    public string Status { get; set; } = default!;           // "completed", "failed", "completed_with_warnings"
    public DateTime FinishedAt { get; set; }
    public List<RoutineResultDto> Results { get; set; } = default!;
    public List<string> Logs { get; set; } = default!;
}

public class RoutineResultDto
{
    public string Pin { get; set; } = default!;
    public string Status { get; set; } = default!;          // "ok", "cancelled", "error"
}