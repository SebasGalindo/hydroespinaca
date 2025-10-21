namespace HydroEspinaca.Shared.DTOs.Actuator;

/// <summary>
/// Individual actuator control command
/// Replaces the previous RoutineStepDto with direct actuatorCode reference
/// </summary>
public class ActuatorControlDto
{
    public string ActuatorCode { get; set; } = default!;  // Actuator.Code (e.g., "Ventiladores", "CalefactorAgua")
    public string? Power { get; set; }                     // "ON" or "OFF" for digital actuators
    public double? DutyCycle { get; set; }                 // 0-100 for PWM actuators
    public double Duration { get; set; }                   // Duration in seconds
}
