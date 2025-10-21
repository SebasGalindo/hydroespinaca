namespace HydroEspinaca.Shared.DTOs.Actuator;

/// <summary>
/// Request to execute multiple independent actuator commands
/// Replaces MultiRoutineCommandDto with simpler command-based model
/// </summary>
public class ExecuteCommandsDto
{
    public List<ActuatorControlDto> Commands { get; set; } = new();
}
