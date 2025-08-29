using HydroEspinaca.Shared.DTOs.Actuator;

namespace ActuatorService.Application.Interfaces;

public interface IJobScheduleStateManager
{
    void AddRoutineToSchedule(string esp32Id, JobRoutineState routine, int channelId);
    JobStatusDto GetJobStatus(string? esp32Id = null);
    void UpdateCommandStatus(string commandId, string status);
    List<string> GetActiveEsp32Ids();
    JobScheduleDto GetCurrentJobSchedule(string esp32Id);
}

public class JobRoutineState
{
    public string CommandId { get; set; } = default!;
    public List<JobStepState> Steps { get; set; } = new();
    public string Status { get; set; } = "scheduled";
}

public class JobStepState
{
    public string Pin { get; set; } = default!;
    public string Mode { get; set; } = default!;
    public string? Power { get; set; }
    public int? DutyCycle { get; set; }
    public int Duration { get; set; }
    public string ActuatorId { get; set; } = default!;
}