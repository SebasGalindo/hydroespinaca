using ActuatorService.Application.Interfaces;
using HydroEspinaca.Shared.Constants;
using HydroEspinaca.Shared.DTOs.Actuator;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace ActuatorService.Application.Services;

public class JobScheduleStateManager : IJobScheduleStateManager
{
    private readonly ILogger<JobScheduleStateManager> _logger;
    
    // In-memory job schedule representation
    private readonly ConcurrentDictionary<string, JobScheduleState> _jobSchedules = new();

    public JobScheduleStateManager(ILogger<JobScheduleStateManager> logger)
    {
        _logger = logger;
    }

    public void AddRoutineToSchedule(string esp32Id, JobRoutineState routine, int channelId)
    {
        var jobScheduleState = _jobSchedules.GetOrAdd(esp32Id, _ => new JobScheduleState(esp32Id));
        var channel = jobScheduleState.Channels.GetOrAdd(channelId, _ => new ChannelState { ChannelId = channelId });
        
        channel.Queue.Add(routine);
        _logger.LogInformation("Added routine {CommandId} to channel {ChannelId} for ESP32 {Esp32Id}", 
            routine.CommandId, channelId, esp32Id);
    }

    public JobStatusDto GetJobStatus(string? esp32Id = null)
    {
        if (!string.IsNullOrEmpty(esp32Id))
        {
            if (_jobSchedules.TryGetValue(esp32Id, out var scheduleState))
            {
                return ConvertToJobStatusDto(scheduleState);
            }
            return new JobStatusDto { Esp32Id = esp32Id, Channels = new() };
        }

        // Return status for all ESP32s - for simplicity, return the first one or empty if none
        var allStatuses = _jobSchedules.Values.Select(ConvertToJobStatusDto).ToList();
        return allStatuses.FirstOrDefault() ?? new JobStatusDto { Esp32Id = "none", Channels = new() };
    }

    public void UpdateCommandStatus(string commandId, string status)
    {
        foreach (var scheduleState in _jobSchedules.Values)
        {
            foreach (var channel in scheduleState.Channels.Values)
            {
                var command = channel.Queue.FirstOrDefault(c => c.CommandId == commandId);
                if (command != null)
                {
                    command.Status = status;
                    _logger.LogInformation("Updated command {CommandId} status to {Status}", commandId, status);
                    return;
                }
            }
        }
    }

    public void UpdateCommandWithNotification(string commandId, string status, string notificationLog)
    {
        foreach (var scheduleState in _jobSchedules.Values)
        {
            foreach (var channel in scheduleState.Channels.Values)
            {
                var command = channel.Queue.FirstOrDefault(c => c.CommandId == commandId);
                if (command != null)
                {
                    command.Status = status;
                    command.NotificationLogs.Add($"{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss}Z: {notificationLog}");
                    _logger.LogInformation("Updated command {CommandId} status to {Status} with notification", commandId, status);
                    return;
                }
            }
        }
        
        _logger.LogWarning("Could not find command {CommandId} to update with notification", commandId);
    }

    public void RemoveCompletedRoutine(string commandId)
    {
        foreach (var scheduleState in _jobSchedules.Values)
        {
            foreach (var channel in scheduleState.Channels.Values)
            {
                var routineToRemove = channel.Queue.FirstOrDefault(c => c.CommandId == commandId);
                if (routineToRemove != null)
                {
                    channel.Queue.Remove(routineToRemove);
                    _logger.LogInformation("Removed completed routine {CommandId} from in-memory job schedule", commandId);
                    return;
                }
            }
        }
        
        _logger.LogWarning("Could not find routine {CommandId} to remove from in-memory job schedule", commandId);
    }

    public void MarkNextCommandAsRunning(string esp32Id, int channelId)
    {
        if (_jobSchedules.TryGetValue(esp32Id, out var scheduleState))
        {
            if (scheduleState.Channels.TryGetValue(channelId, out var channel))
            {
                // Check if there's already a command running in this channel
                var runningCommand = channel.Queue.FirstOrDefault(c => c.Status == ActuatorConstants.CommandStatuses.Running);
                if (runningCommand != null)
                {
                    _logger.LogDebug("Channel {ChannelId} already has a running command: {CommandId}", channelId, runningCommand.CommandId);
                    return;
                }

                // Mark the first scheduled command as running
                var nextCommand = channel.Queue.FirstOrDefault(c => c.Status == ActuatorConstants.CommandStatuses.Scheduled);
                if (nextCommand != null)
                {
                    nextCommand.Status = ActuatorConstants.CommandStatuses.Running;
                    _logger.LogInformation("Marked command {CommandId} as running in channel {ChannelId}", nextCommand.CommandId, channelId);
                }
                else
                {
                    _logger.LogInformation("No scheduled commands found in channel {ChannelId} for ESP32 {Esp32Id}", channelId, esp32Id);
                }
            }
        }
    }

    public List<string> GetActiveEsp32Ids()
    {
        return _jobSchedules.Keys.ToList();
    }

    public JobScheduleDto GetCurrentJobSchedule(string esp32Id)
    {
        if (_jobSchedules.TryGetValue(esp32Id, out var scheduleState))
        {
            return ConvertToJobScheduleDto(scheduleState);
        }

        return new JobScheduleDto { Esp32Id = esp32Id, JobSchedule = new() };
    }

    private JobScheduleDto ConvertToJobScheduleDto(JobScheduleState state)
    {
        var channels = new List<JobChannelDto>();
        
        for (int i = ActuatorConstants.Channels.MinChannelId; i <= ActuatorConstants.Channels.MaxChannelId; i++)
        {
            var channelQueue = new List<JobRoutineDto>();
            
            if (state.Channels.TryGetValue(i, out var channel))
            {
                channelQueue = channel.Queue.Select(r => new JobRoutineDto
                {
                    CommandId = r.CommandId,
                    Steps = r.Steps.Select(s => new JobStepDto
                    {
                        Pin = s.Pin,
                        Mode = s.Mode,
                        Power = s.Power,
                        DutyCycle = s.DutyCycle,
                        Duration = s.Duration
                    }).ToList()
                }).ToList();
            }

            channels.Add(new JobChannelDto
            {
                Channel = i,
                Queue = channelQueue
            });
        }

        return new JobScheduleDto
        {
            Esp32Id = state.Esp32Id,
            JobSchedule = channels
        };
    }

    private JobStatusDto ConvertToJobStatusDto(JobScheduleState state)
    {
        var channels = new List<ChannelStatusDto>();
        
        for (int i = ActuatorConstants.Channels.MinChannelId; i <= ActuatorConstants.Channels.MaxChannelId; i++)
        {
            var queuedCommands = new List<QueuedCommandDto>();
            
            if (state.Channels.TryGetValue(i, out var channel))
            {
                queuedCommands = channel.Queue.Select(r => new QueuedCommandDto
                {
                    CommandId = r.CommandId,
                    Status = r.Status
                }).ToList();
            }

            channels.Add(new ChannelStatusDto
            {
                Channel = i,
                Queue = queuedCommands
            });
        }

        return new JobStatusDto
        {
            Esp32Id = state.Esp32Id,
            Channels = channels
        };
    }
}

// Internal state classes
internal class JobScheduleState
{
    public string Esp32Id { get; set; }
    public ConcurrentDictionary<int, ChannelState> Channels { get; set; } = new();

    public JobScheduleState(string esp32Id)
    {
        Esp32Id = esp32Id;
    }
}

internal class ChannelState
{
    public int ChannelId { get; set; }
    public List<JobRoutineState> Queue { get; set; } = new();
}