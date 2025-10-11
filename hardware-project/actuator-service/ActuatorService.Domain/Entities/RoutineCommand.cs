using HydroEspinaca.Shared.Abstractions;
using HydroEspinaca.Shared.Enums;
using System.Text.Json.Serialization;

namespace ActuatorService.Domain.Entities;

public class RoutineCommand : IIdentifiableMutable
{
    public string Id { get; private set; } = default!;        // Auto-generated
    public string CommandId { get; set; } = default!;         // {routineId}_{timestamp}
    public string RoutineId { get; set; } = default!;
    public string Esp32Id { get; set; } = default!;           // ESP32 device identifier
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public RoutineCommandStatus StatusGeneral { get; set; } = RoutineCommandStatus.SCHEDULED;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? FinishedAt { get; set; }
    public int? Channel { get; set; }                         // Channel assignment (1-3)
    public List<RoutineStepMapping>? StepMappings { get; set; }  // Maps Pin → ActuatorId for state tracking
    public List<RoutineResult>? Results { get; set; }

    public void SetId(string id) => Id = id;
}

public class RoutineStepMapping
{
    public string Pin { get; set; } = default!;
    public string ActuatorId { get; set; } = default!;
    public string OutputVariableId { get; set; } = default!;

    /// <summary>
    /// Duration in seconds for this step
    /// </summary>
    public double Duration { get; set; }

    /// <summary>
    /// Duty cycle for PWM actuators (0-100%)
    /// </summary>
    public double? DutyCycle { get; set; }

    /// <summary>
    /// Power state for DIGITAL actuators
    /// </summary>
    public string? Power { get; set; }
}

public class RoutineResult
{
    public string Pin { get; set; } = default!;
    public string Status { get; set; } = default!;  // ok, cancelled, error
    public List<string>? ExecutionLog { get; set; }
}