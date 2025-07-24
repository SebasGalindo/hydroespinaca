using HydroEspinaca.Shared.Enums;

namespace HydroEspinaca.Shared.DTOs.Actuator;
public class ActuatorCommandDto
{
    public string Id { get; set; }
    public string ActuatorId { get; set; }
    public string Esp32Id { get; set; }
    public string Action { get; set; }
    public int? DurationMs { get; set; }
    public TriggerType Trigger { get; set; }
    public string? RoutineId { get; set; }
    public int? RoutineStepOrder { get; set; }
    public CommandMetadataDto? Metadata { get; set; }
    public DateTime Timestamp { get; set; }
}