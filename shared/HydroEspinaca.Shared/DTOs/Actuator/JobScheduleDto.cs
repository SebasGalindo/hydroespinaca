namespace HydroEspinaca.Shared.DTOs.Actuator;

public class JobScheduleDto
{
    public string Esp32Id { get; set; } = default!;
    public List<JobChannelDto> JobSchedule { get; set; } = new();
}

public class JobChannelDto
{
    public int Channel { get; set; }
    public List<JobRoutineDto> Queue { get; set; } = new();
}

public class JobRoutineDto
{
    public string CommandId { get; set; } = default!;
    public List<JobStepDto> Steps { get; set; } = new();
}

public class JobStepDto
{
    public string Pin { get; set; } = default!;
    public string Mode { get; set; } = default!;      // "DIGITAL" or "PWM"
    public string? Power { get; set; }                // "ON" or "OFF" for digital
    public int? DutyCycle { get; set; }               // 0-100 for PWM
    public int Duration { get; set; }                 // Duration in seconds
}

public class JobStatusDto
{
    public string Esp32Id { get; set; } = default!;
    public List<ChannelStatusDto> Channels { get; set; } = new();
}

public class ChannelStatusDto
{
    public int Channel { get; set; }
    public List<QueuedCommandDto> Queue { get; set; } = new();
}

public class QueuedCommandDto
{
    public string CommandId { get; set; } = default!;
    public string Status { get; set; } = default!;    // "scheduled", "running", "completed"
}