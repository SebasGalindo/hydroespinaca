using ActuatorService.Domain.Entities;
using HydroEspinaca.Shared.DTOs.Actuator;

namespace ActuatorService.Application.Models;

/// <summary>
/// Represents a routine that is waiting for pin availability.
/// Will be activated when all required pins become free.
/// </summary>
public record PendingRoutine
{
    public string CommandId { get; init; } = default!;
    public string RoutineId { get; init; } = default!;
    public string Esp32Id { get; init; } = default!;
    public IReadOnlyList<string> Pins { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> ActuatorIds { get; init; } = Array.Empty<string>();
    public DateTime ScheduledTime { get; init; }
    public List<RoutineStepMapping> StepMappings { get; init; } = new();
    public JobRoutineDto JobRoutineDto { get; init; } = default!; // For MQTT publishing
}
