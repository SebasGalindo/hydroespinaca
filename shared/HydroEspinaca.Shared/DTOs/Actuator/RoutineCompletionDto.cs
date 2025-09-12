using HydroEspinaca.Shared.Constants;
using HydroEspinaca.Shared.Enums;

namespace HydroEspinaca.Shared.DTOs.Actuator;

public class RoutineCompletionDto
{
    public string Esp32Id { get; set; } = default!;
    public string CommandId { get; set; } = default!;
    public List<RoutineStepResultDto> Steps { get; set; } = default!;
    public RoutineCommandStatus Status => DetermineOverallStatus();
    public DateTime FinishedAt { get; set; } = DateTime.UtcNow;

    private RoutineCommandStatus DetermineOverallStatus()
    {
        if (Steps == null || Steps.Count == 0)
            return RoutineCommandStatus.FAILED;

        var hasErrors = Steps.Any(s => s.Status == ActuatorConstants.StepStatuses.Error);
        var hasCancelled = Steps.Any(s => s.Status == ActuatorConstants.StepStatuses.Cancelled);
        var allOk = Steps.All(s => s.Status == ActuatorConstants.StepStatuses.Ok);

        if (hasErrors)
            return RoutineCommandStatus.FAILED;
        if (hasCancelled && !allOk)
            return RoutineCommandStatus.CANCELLED;
        if (allOk)
            return RoutineCommandStatus.COMPLETED;

        return RoutineCommandStatus.FAILED; // fallback
    }
}


public class RoutineStepResultDto
{
    public int Pin { get; set; }
    public string Status { get; set; } = default!;          // "ok", "cancelled", "error"
    public List<string>? ExecutionLog { get; set; }
}