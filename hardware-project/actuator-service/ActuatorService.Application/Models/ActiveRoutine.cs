using ActuatorService.Domain.Entities;

namespace ActuatorService.Application.Models;

/// <summary>
/// Represents a routine that is currently being executed by the firmware.
/// Tracks pins in use and execution timing.
/// </summary>
public record ActiveRoutine
{
    public string CommandId { get; init; } = default!;
    public string RoutineId { get; init; } = default!;
    public string Esp32Id { get; init; } = default!;
    public IReadOnlyList<string> Pins { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> ActuatorIds { get; init; } = Array.Empty<string>();
    public DateTime StartTime { get; init; }
    public List<RoutineStepMapping> StepMappings { get; init; } = new();
}
