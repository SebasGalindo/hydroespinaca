namespace ActuatorService.Application.DTOs;

public record RoutineExecutionStats
{
    public int ActiveCount { get; init; }
    public int PendingCount { get; init; }
    public int TotalLockedPins { get; init; }
    public List<string> Esp32Ids { get; init; } = new();
}
