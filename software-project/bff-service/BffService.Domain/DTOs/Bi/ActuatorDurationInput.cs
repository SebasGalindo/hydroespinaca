namespace BffService.Domain.DTOs.Bi;

/// <summary>
/// Input sent by BFF to bi-service with pre-combined actuator data.
/// Mirrors BiService.Application.DTOs.OperationalCost.ActuatorDurationInput.
/// </summary>
public class ActuatorDurationInput
{
    public string ActuatorCode { get; set; } = string.Empty;
    public decimal PowerConsumptionWatts { get; set; }
    public double TotalDurationSeconds { get; set; }
    public int ActivationCount { get; set; }
}
