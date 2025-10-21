namespace HydroEspinaca.Shared.DTOs.Actuator;

/// <summary>
/// Represents a command completion notification from the firmware via MQTT.
/// When received, the command should be marked as FINISHED in the database.
/// </summary>
public class RoutineCompletionDto
{
    public string Esp32Id { get; set; } = default!;
    public string CommandId { get; set; } = default!;

    /// <summary>
    /// Status from firmware: "completed", "cancelled", "error", etc.
    /// This will be mapped to RoutineCommandStatus.FINISHED in the handler.
    /// </summary>
    public string Status { get; set; } = default!;
}