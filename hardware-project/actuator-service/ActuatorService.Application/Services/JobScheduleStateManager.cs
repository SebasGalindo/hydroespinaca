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

    public void AddRoutineToSchedule(string esp32Id, JobRoutineState routine, int channelId, int priority = 0)
    {
        var jobScheduleState = _jobSchedules.GetOrAdd(esp32Id, _ => new JobScheduleState(esp32Id));
        var channel = jobScheduleState.Channels.GetOrAdd(channelId, _ => new ChannelState { ChannelId = channelId });
        
        routine.Priority = priority;
        
        // Insert based on priority, duration, and existing queue state
        if (priority == 2) // Control priority - interrupt execution if possible
        {
            // Control commands get highest priority - insert right after running commands
            var runningIndex = channel.Queue.FindIndex(r => r.Status == ActuatorConstants.CommandStatuses.Running);
            var insertIndex = runningIndex >= 0 ? runningIndex + 1 : 0;
            
            // Among control commands, insert by duration (shorter first)
            var controlCommands = channel.Queue.Skip(insertIndex).TakeWhile(r => r.Priority == 2).ToList();
            var totalDuration = routine.Steps.Sum(s => s.Duration);
            var positionInControl = controlCommands.FindIndex(r => r.Steps.Sum(s => s.Duration) > totalDuration);
            
            if (positionInControl >= 0)
            {
                insertIndex += positionInControl;
            }
            else
            {
                insertIndex += controlCommands.Count;
            }
            
            channel.Queue.Insert(insertIndex, routine);
            _logger.LogInformation("Inserted CONTROL routine {CommandId} at position {Position} in channel {ChannelId} for ESP32 {Esp32Id} (duration: {Duration}s)", 
                routine.CommandId, insertIndex, channelId, esp32Id, totalDuration);
        }
        else if (priority == 1) // SingleStep priority - insert before normal routines, ordered by duration
        {
            var insertIndex = channel.Queue.FindIndex(r => r.Priority == 0);
            if (insertIndex >= 0)
            {
                // Find correct position among single-step routines (ordered by duration)
                var singleStepStart = channel.Queue.FindIndex(r => r.Priority == 1);
                if (singleStepStart >= 0)
                {
                    var totalDuration = routine.Steps.Sum(s => s.Duration);
                    var singleStepCommands = channel.Queue.Skip(singleStepStart).TakeWhile(r => r.Priority >= 1).Where(r => r.Priority == 1).ToList();
                    var positionInSingleStep = singleStepCommands.FindIndex(r => r.Steps.Sum(s => s.Duration) > totalDuration);
                    
                    if (positionInSingleStep >= 0)
                    {
                        insertIndex = singleStepStart + positionInSingleStep;
                    }
                    else
                    {
                        insertIndex = singleStepStart + singleStepCommands.Count;
                    }
                }
                channel.Queue.Insert(insertIndex, routine);
            }
            else
            {
                channel.Queue.Add(routine);
            }
            var duration = routine.Steps.Sum(s => s.Duration);
            _logger.LogInformation("Inserted SINGLE-STEP routine {CommandId} with priority in channel {ChannelId} for ESP32 {Esp32Id} (duration: {Duration}s)", 
                routine.CommandId, channelId, esp32Id, duration);
        }
        else // Normal priority - insert ordered by duration among normal routines
        {
            var totalDuration = routine.Steps.Sum(s => s.Duration);
            var normalStart = channel.Queue.FindIndex(r => r.Priority == 0);
            
            if (normalStart >= 0)
            {
                var normalCommands = channel.Queue.Skip(normalStart).Where(r => r.Priority == 0).ToList();
                var positionInNormal = normalCommands.FindIndex(r => r.Steps.Sum(s => s.Duration) > totalDuration);
                
                if (positionInNormal >= 0)
                {
                    channel.Queue.Insert(normalStart + positionInNormal, routine);
                }
                else
                {
                    channel.Queue.Add(routine);
                }
            }
            else
            {
                channel.Queue.Add(routine);
            }
            
            _logger.LogInformation("Added NORMAL routine {CommandId} to channel {ChannelId} for ESP32 {Esp32Id} ordered by duration ({Duration}s)", 
                routine.CommandId, channelId, esp32Id, totalDuration);
        }
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

    public JobScheduleState? GetInternalScheduleState(string esp32Id)
    {
        return _jobSchedules.TryGetValue(esp32Id, out var scheduleState) ? scheduleState : null;
    }

    public void ClearJobSchedule(string? esp32Id = null)
    {
        if (esp32Id != null)
        {
            if (_jobSchedules.TryRemove(esp32Id, out var removed))
            {
                _logger.LogInformation("🧹 Cleared job schedule for ESP32: {Esp32Id}", esp32Id);
            }
            else
            {
                _logger.LogWarning("⚠️  No job schedule found for ESP32: {Esp32Id}", esp32Id);
            }
        }
        else
        {
            var clearedCount = _jobSchedules.Count;
            _jobSchedules.Clear();
            _logger.LogInformation("🧹 Cleared all job schedules - {Count} ESP32s", clearedCount);
        }
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
                    BaseId = r.BaseId,
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

