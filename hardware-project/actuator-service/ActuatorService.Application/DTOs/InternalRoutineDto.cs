namespace ActuatorService.Application.DTOs;

/// <summary>
/// DTO for internal routine information
/// </summary>
public class InternalRoutineDto
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string Esp32Id { get; set; } = default!;
    public TimeSpan Interval { get; set; }
    public TimeSpan StartTime { get; set; }
    public List<InternalRoutineStepDto> Steps { get; set; } = new();
    public DateTime? LastExecutedAt { get; set; }
    public DateTime? NextExecutionEstimate { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// DTO for creating or updating an internal routine
/// </summary>
public class CreateInternalRoutineDto
{
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string Esp32Id { get; set; } = default!;
    public TimeSpan Interval { get; set; }
    public TimeSpan StartTime { get; set; }
    public List<InternalRoutineStepDto> Steps { get; set; } = new();
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// DTO for updating an internal routine
/// </summary>
public class UpdateInternalRoutineDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Esp32Id { get; set; }
    public TimeSpan? Interval { get; set; }
    public TimeSpan? StartTime { get; set; }
    public List<InternalRoutineStepDto>? Steps { get; set; }
    public bool? IsActive { get; set; }
}

/// <summary>
/// DTO for internal routine step
/// </summary>
public class InternalRoutineStepDto
{
    public string OutputVariable { get; set; } = default!;
    public string Power { get; set; } = default!;
    public double Duration { get; set; }
    public double? DutyCycle { get; set; }
    public string? Mode { get; set; }
}
