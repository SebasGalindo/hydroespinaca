namespace HydroEspinaca.Shared.DTOs.Actuator;

/// <summary>
/// Information about an internal routine scheduled in the system
/// </summary>
public class InternalRoutineInfoDto
{
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string Interval { get; set; } = default!;
    public DateTime? NextExecutionEstimate { get; set; }
    public bool IsActive { get; set; }
}
