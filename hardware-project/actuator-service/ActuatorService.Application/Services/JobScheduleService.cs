using ActuatorService.Application.DTOs;
using ActuatorService.Application.Interfaces;
using ActuatorService.Domain.Exceptions;
using HydroEspinaca.Shared.Constants;
using HydroEspinaca.Shared.DTOs.Actuator;
using Microsoft.Extensions.Logging;

namespace ActuatorService.Application.Services;

public class JobScheduleService : IJobScheduleService
{
    private readonly IJobScheduleStateManager _stateManager;
    private readonly ILogger<JobScheduleService> _logger;

    public JobScheduleService(
        IJobScheduleStateManager stateManager,
        ILogger<JobScheduleService> _logger)
    {
        _stateManager = stateManager;
        this._logger = _logger;
    }

    public Task<JobScheduleDto> CreateJobScheduleAsync(List<ResolvedRoutineDto> resolvedRoutines)
    {
        _logger.LogInformation("Creating job schedule for {RoutineCount} resolved routines", resolvedRoutines.Count);

        if (!resolvedRoutines.Any())
        {
            throw new ArgumentException("No routines provided for scheduling");
        }

        // All routines should belong to same ESP32 (validated earlier)
        var esp32Id = resolvedRoutines.First().Esp32Id;

        // Process each resolved routine through the scheduling algorithm
        foreach (var resolvedRoutine in resolvedRoutines)
        {
            AssignRoutineToChannel(esp32Id, resolvedRoutine);
        }

        // Mark ONLY the first routine in each channel as running (respecting existing queue)
        for (int channelId = ActuatorConstants.Channels.MinChannelId; channelId <= ActuatorConstants.Channels.MaxChannelId; channelId++)
        {
            _stateManager.MarkNextCommandAsRunning(esp32Id, channelId);
        }

        return Task.FromResult(_stateManager.GetCurrentJobSchedule(esp32Id));
    }

    public Task<JobScheduleDto> CreateJobScheduleAsync(List<RoutineCommandDto> routines)
    {
        throw new NotSupportedException("Use CreateJobScheduleAsync with ResolvedRoutineDto instead");
    }

    public Task<JobStatusDto> GetJobStatusAsync(string? esp32Id = null)
    {
        return Task.FromResult(_stateManager.GetJobStatus(esp32Id));
    }

    public Task UpdateChannelStatusAsync(string commandId, string status)
    {
        _stateManager.UpdateCommandStatus(commandId, status);
        return Task.CompletedTask;
    }

    public Task<List<string>> GetActiveEsp32IdsAsync()
    {
        return Task.FromResult(_stateManager.GetActiveEsp32Ids());
    }

    public Task ClearJobScheduleAsync(string? esp32Id = null)
    {
        _stateManager.ClearJobSchedule(esp32Id);
        return Task.CompletedTask;
    }

    private void AssignRoutineToChannel(string esp32Id, ResolvedRoutineDto resolvedRoutine)
    {
        var commandId = GenerateCommandId(resolvedRoutine.RoutineId);

        // Transform resolved steps to job step state
        var jobSteps = resolvedRoutine.ResolvedSteps.Select(rs => new JobStepState
        {
            Pin = rs.Pin,
            Mode = rs.Mode.ToString(),
            Power = rs.Power,
            DutyCycle = rs.DutyCycle,
            Duration = rs.Duration,
            ActuatorId = rs.ActuatorId
        }).ToList();

        var jobRoutine = new JobRoutineState
        {
            CommandId = commandId,
            BaseId = resolvedRoutine.RoutineId,
            Steps = jobSteps,
            Status = ActuatorConstants.CommandStatuses.Scheduled // Always start as SCHEDULED, will be updated if needed
        };

        // Determine priority level
        var priority = DeterminePriority(resolvedRoutine);

        // Find the best channel for this routine (prevents GPIO conflicts)
        var channelId = FindBestChannelForRoutine(resolvedRoutine, priority);

        // Add to the selected channel via state manager with priority
        _stateManager.AddRoutineToSchedule(esp32Id, jobRoutine, channelId, (int)priority);

        _logger.LogInformation("Assigned routine {CommandId} to channel {ChannelId} with priority {Priority}",
            commandId, channelId, priority);
    }

    private string GenerateCommandId(string routineId)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddTHHmmss");
        return $"{routineId}_{timestamp}";
    }

    /// <summary>
    /// Determines the priority of a routine based on its characteristics.
    /// Priority rules:
    /// - Control (2): All steps have power=Off or dutyCycle=0. Interrupts execution and allows duration=0.
    /// - SingleStep (1): Routines with exactly one step. Higher priority than normal routines.
    /// - Normal (0): Multi-step routines without control characteristics.
    /// </summary>
    private RoutinePriority DeterminePriority(ResolvedRoutineDto resolvedRoutine)
    {
        // Control routines: all steps have "OFF" power or 0 duty cycle
        bool isControl = resolvedRoutine.ResolvedSteps.All(step =>
            (step.Power == ActuatorConstants.PowerStates.Off) || (step.DutyCycle == ActuatorConstants.Validation.MinDutyCycle));

        if (isControl)
            return RoutinePriority.Control;

        // Single step routines get medium-high priority
        if (resolvedRoutine.ResolvedSteps.Count == 1)
            return RoutinePriority.SingleStep;

        return RoutinePriority.Normal;
    }

    private int FindBestChannelForRoutine(ResolvedRoutineDto resolvedRoutine, RoutinePriority priority)
    {
        var routineActuators = resolvedRoutine.ResolvedSteps.Select(s => s.ActuatorId).ToHashSet();
        
        // 1. CRITICAL: Find channels that already have ANY of the same actuators
        //    All routines using the same actuator/pin MUST go to the same channel to prevent GPIO conflicts
        for (int channelId = ActuatorConstants.Channels.MinChannelId; channelId <= ActuatorConstants.Channels.MaxChannelId; channelId++)
        {
            var channelActuators = GetChannelActuatorIds(channelId);
            if (channelActuators.Any() && routineActuators.Overlaps(channelActuators))
            {
                _logger.LogInformation("🎯 Assigning routine to channel {ChannelId} - found matching actuators: {MatchingActuators}", 
                    channelId, string.Join(", ", routineActuators.Intersect(channelActuators)));
                return channelId;
            }
        }
        
        // 2. No actuator conflicts - find channel with minimum load among safe channels
        var routinePins = GetRoutinePins(resolvedRoutine);
        var safeChannels = new List<(int channelId, int load)>();

        for (int channelId = ActuatorConstants.Channels.MinChannelId; channelId <= ActuatorConstants.Channels.MaxChannelId; channelId++)
        {
            var channelPins = GetChannelPins(channelId);

            // Check if there's any GPIO pin conflict
            if (channelPins.Any() && routinePins.Overlaps(channelPins))
            {
                _logger.LogDebug("⚠️  Channel {ChannelId} has GPIO pin conflicts: {ConflictingPins}",
                    channelId, string.Join(", ", routinePins.Intersect(channelPins)));
                continue; // Skip this channel due to GPIO conflict
            }

            // This channel is safe - add to candidates with its current load
            var load = GetChannelLoad(channelId);
            safeChannels.Add((channelId, load));
            _logger.LogDebug("✅ Channel {ChannelId} is safe - current load: {Load}", channelId, load);
        }
        
        // 3. Choose the safe channel with minimum load for load balancing
        if (safeChannels.Any())
        {
            var bestChannel = safeChannels.OrderBy(c => c.load).First();
            _logger.LogInformation("🎯 Assigning routine to channel {ChannelId} for load balancing (load: {Load})", 
                bestChannel.channelId, bestChannel.load);
            return bestChannel.channelId;
        }
        
        // 4. All channels have GPIO conflicts - find the one with minimum load as last resort
        _logger.LogWarning("⚠️  All channels have GPIO conflicts - using channel with minimum load");
        var allChannelLoads = new List<(int channelId, int load)>();
        for (int channelId = ActuatorConstants.Channels.MinChannelId; channelId <= ActuatorConstants.Channels.MaxChannelId; channelId++)
        {
            allChannelLoads.Add((channelId, GetChannelLoad(channelId)));
        }
        
        var fallbackChannel = allChannelLoads.OrderBy(cl => cl.load).First();
        _logger.LogWarning("📍 Using fallback channel {ChannelId} with load {Load}", fallbackChannel.channelId, fallbackChannel.load);
        return fallbackChannel.channelId;
    }
    
    private HashSet<string> GetChannelActuatorIds(int channelId)
    {
        // Get all actuator IDs currently in this channel across all ESP32s
        // We need to access the internal state that has ActuatorId, not the DTO conversion
        var actuatorIds = new HashSet<string>();
        foreach (var esp32Id in _stateManager.GetActiveEsp32Ids())
        {
            // Get the internal state which has ActuatorId in JobStepState
            var internalSchedule = _stateManager.GetInternalScheduleState(esp32Id);
            if (internalSchedule != null && internalSchedule.Channels.TryGetValue(channelId, out var channel))
            {
                foreach (var routine in channel.Queue)
                {
                    foreach (var step in routine.Steps)
                    {
                        actuatorIds.Add(step.ActuatorId);
                    }
                }
            }
        }
        return actuatorIds;
    }

    private HashSet<string> GetChannelPins(int channelId)
    {
        // Get all GPIO pins currently in use by this channel across all ESP32s
        var pins = new HashSet<string>();
        foreach (var esp32Id in _stateManager.GetActiveEsp32Ids())
        {
            var schedule = _stateManager.GetCurrentJobSchedule(esp32Id);
            var channel = schedule.JobSchedule.FirstOrDefault(c => c.Channel == channelId);
            if (channel != null)
            {
                foreach (var routine in channel.Queue)
                {
                    foreach (var step in routine.Steps)
                    {
                        pins.Add(step.Pin);
                    }
                }
            }
        }
        return pins;
    }

    private HashSet<string> GetRoutinePins(ResolvedRoutineDto resolvedRoutine)
    {
        // Get all GPIO pins that this routine will use (already resolved)
        return resolvedRoutine.ResolvedSteps.Select(s => s.Pin).ToHashSet();
    }
    
    private bool IsChannelFree(int channelId)
    {
        foreach (var esp32Id in _stateManager.GetActiveEsp32Ids())
        {
            var schedule = _stateManager.GetCurrentJobSchedule(esp32Id);
            var channel = schedule.JobSchedule.FirstOrDefault(c => c.Channel == channelId);
            if (channel?.Queue.Any() == true)
            {
                return false;
            }
        }
        return true;
    }
    
    private int GetChannelLoad(int channelId)
    {
        int totalLoad = 0;
        foreach (var esp32Id in _stateManager.GetActiveEsp32Ids())
        {
            var schedule = _stateManager.GetCurrentJobSchedule(esp32Id);
            var channel = schedule.JobSchedule.FirstOrDefault(c => c.Channel == channelId);
            totalLoad += channel?.Queue.Count ?? 0;
        }
        return totalLoad;
    }
}

internal enum RoutinePriority
{
    Normal = 0,
    SingleStep = 1,
    Control = 2
}