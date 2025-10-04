namespace HydroEspinaca.Shared.DTOs.Actuator;

public class RoutineCommandDto
{
    public string RoutineId { get; set; } = default!;
    public List<RoutineStepDto> Steps { get; set; } = default!;
}

public class RoutineStepDto
{
    public string OutputVariable { get; set; } = default!;  // Control Output ID from control_outputs collection
    public string? Power { get; set; }                      // "ON" or "OFF" for digital actuators
    public int? DutyCycle { get; set; }                     // 0-100 for PWM actuators
    public int Duration { get; set; }                       // Duration in seconds
}