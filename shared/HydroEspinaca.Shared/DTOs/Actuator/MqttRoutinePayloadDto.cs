namespace HydroEspinaca.Shared.DTOs.Actuator;

public class MqttRoutinePayloadDto
{
    public string RoutineId { get; set; } = default!;
    public List<MqttStepDto> Steps { get; set; } = default!;
}

public class MqttStepDto
{
    public string Pin { get; set; } = default!;
    public string Mode { get; set; } = default!;      // "DIGITAL" or "PWM"
    public string? Power { get; set; }                // "ON" or "OFF" for digital
    public double? DutyCycle { get; set; }            // 0-100 for PWM (supports decimals)
    public double Duration { get; set; }              // Duration in seconds (supports decimals)
}