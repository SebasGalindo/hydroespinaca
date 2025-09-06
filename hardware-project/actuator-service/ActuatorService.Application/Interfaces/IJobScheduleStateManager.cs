using HydroEspinaca.Shared.Constants;
using HydroEspinaca.Shared.DTOs.Actuator;
using System.Collections.Concurrent;

namespace ActuatorService.Application.Interfaces;

public interface IJobScheduleStateManager
{
    void AddRoutineToSchedule(string esp32Id, JobRoutineState routine, int channelId, int priority = 0);
    JobStatusDto GetJobStatus(string? esp32Id = null);
    void UpdateCommandStatus(string commandId, string status);
    void RemoveCompletedRoutine(string commandId);
    void MarkNextCommandAsRunning(string esp32Id, int channelId);
    List<string> GetActiveEsp32Ids();
    JobScheduleDto GetCurrentJobSchedule(string esp32Id);
    JobScheduleState? GetInternalScheduleState(string esp32Id);
    void ClearJobSchedule(string? esp32Id = null);
}

public class JobRoutineState
{
    public string CommandId { get; set; } = default!;
    public string BaseId { get; set; } = default!;
    public List<JobStepState> Steps { get; set; } = new();
    public string Status { get; set; } = ActuatorConstants.CommandStatuses.Scheduled;
    public int Priority { get; set; } = 0;
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

public class JobScheduleState
{
    public string Esp32Id { get; set; }
    public ConcurrentDictionary<int, ChannelState> Channels { get; set; } = new();

    public JobScheduleState(string esp32Id)
    {
        Esp32Id = esp32Id;
    }
}

public class ChannelState
{
    public int ChannelId { get; set; }
    public List<JobRoutineState> Queue { get; set; } = new();
}