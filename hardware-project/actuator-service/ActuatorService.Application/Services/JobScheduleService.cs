using ActuatorService.Application.Interfaces;
using ActuatorService.Domain.Interfaces;
using HydroEspinaca.Shared.Constants;
using HydroEspinaca.Shared.DTOs.Actuator;
using HydroEspinaca.Shared.Enums;
using HydroEspinaca.Shared.Errors;
using Microsoft.Extensions.Logging;

namespace ActuatorService.Application.Services;

public class JobScheduleService : IJobScheduleService
{
    private readonly IActuatorRepository _actuatorRepository;
    private readonly IJobScheduleStateManager _stateManager;
    private readonly ILogger<JobScheduleService> _logger;

    public JobScheduleService(
        IActuatorRepository actuatorRepository,
        IJobScheduleStateManager stateManager,
        ILogger<JobScheduleService> logger)
    {
        _actuatorRepository = actuatorRepository;
        _stateManager = stateManager;
        _logger = logger;
    }

    public async Task<JobScheduleDto> CreateJobScheduleAsync(List<RoutineCommandDto> routines)
    {
        _logger.LogInformation("Creating job schedule for {RoutineCount} routines", routines.Count);

        // Group routines by ESP32 ID (assuming all actuators belong to same ESP32 for now)
        var esp32Id = await GetEsp32IdForRoutinesAsync(routines);

        // Process each routine through the scheduling algorithm
        foreach (var routine in routines)
        {
            await AssignRoutineToChannelAsync(esp32Id, routine);
        }

        // Mark ONLY the first routine in each channel as running (respecting existing queue)
        for (int channelId = ActuatorConstants.Channels.MinChannelId; channelId <= ActuatorConstants.Channels.MaxChannelId; channelId++)
        {
            _stateManager.MarkNextCommandAsRunning(esp32Id, channelId);
        }

        return _stateManager.GetCurrentJobSchedule(esp32Id);
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

    private async Task<string> GetEsp32IdForRoutinesAsync(List<RoutineCommandDto> routines)
    {
        var firstActuatorId = routines.SelectMany(r => r.Steps).Select(s => s.Actuator).FirstOrDefault();

        if (firstActuatorId == null)
        {
            throw new NotFoundException(
                $"No se encontró ningún actuador en las rutinas: {string.Join(", ", routines.Select(r => r.RoutineId))}");
        }

        var actuator = await _actuatorRepository.GetByIdAsync(firstActuatorId);

        if (actuator?.Esp32Id == null)
        {
            throw new NotFoundException(
                $"No se pudo determinar el ESP32 asociado al actuador '{firstActuatorId}' en las rutinas: {string.Join(", ", routines.Select(r => r.RoutineId))}");
        }

        return actuator.Esp32Id;
    }



    private async Task AssignRoutineToChannelAsync(string esp32Id, RoutineCommandDto routine)
    {
        var commandId = GenerateCommandId(routine.RoutineId);

        // Enrich steps with actuator data
        var enrichedSteps = await EnrichRoutineStepsAsync(routine.Steps);

        var jobRoutine = new JobRoutineState
        {
            CommandId = commandId,
            BaseId = routine.RoutineId,
            Steps = enrichedSteps,
            Status = ActuatorConstants.CommandStatuses.Scheduled // Always start as SCHEDULED, will be updated if needed
        };

        // Determine priority level
        var priority = DeterminePriority(routine);

        // Find the best channel for this routine (prevents GPIO conflicts)
        var channelId = await FindBestChannelForRoutineAsync(routine, priority);

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

    private async Task<List<JobStepState>> EnrichRoutineStepsAsync(List<RoutineStepDto> steps)
    {
        var enrichedSteps = new List<JobStepState>();

        foreach (var step in steps)
        {
            var actuator = await _actuatorRepository.GetByIdAsync(step.Actuator);
            if (actuator == null)
                throw new InvalidOperationException($"Actuator {step.Actuator} not found");

            var jobStep = new JobStepState
            {
                Pin = actuator.Pin.ToString(),
                Mode = actuator.Mode.ToString(),
                Power = step.Power,
                DutyCycle = step.DutyCycle,
                Duration = step.Duration,
                ActuatorId = step.Actuator
            };

            enrichedSteps.Add(jobStep);
        }

        return enrichedSteps;
    }

    /// <summary>
    /// Determines the priority of a routine based on its characteristics.
    /// Priority rules:
    /// - Control (2): All steps have power=Off or dutyCycle=0. Interrupts execution and allows duration=0.
    /// - SingleStep (1): Routines with exactly one step. Higher priority than normal routines.
    /// - Normal (0): Multi-step routines without control characteristics.
    /// </summary>
    private RoutinePriority DeterminePriority(RoutineCommandDto routine)
    {
        // Control routines: all steps have "OFF" power or 0 duty cycle
        bool isControl = routine.Steps.All(step =>
            (step.Power == ActuatorConstants.PowerStates.Off) || (step.DutyCycle == ActuatorConstants.Validation.MinDutyCycle));

        if (isControl)
            return RoutinePriority.Control;

        // Single step routines get medium-high priority
        if (routine.Steps.Count == 1)
            return RoutinePriority.SingleStep;

        return RoutinePriority.Normal;
    }

    private async Task<int> FindBestChannelForRoutineAsync(RoutineCommandDto routine, RoutinePriority priority)
    {
        var routineActuators = routine.Steps.Select(s => s.Actuator).ToHashSet();
        
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
        
        // 2. No actuator conflicts - try to find channels that don't conflict with our GPIO pins
        for (int channelId = ActuatorConstants.Channels.MinChannelId; channelId <= ActuatorConstants.Channels.MaxChannelId; channelId++)
        {
            var channelPins = GetChannelPins(channelId);
            var routinePins = await GetRoutinePinsAsync(routine);
            
            // Check if there's any GPIO pin conflict
            if (channelPins.Any() && routinePins.Overlaps(channelPins))
            {
                _logger.LogDebug("⚠️  Channel {ChannelId} has GPIO pin conflicts: {ConflictingPins}", 
                    channelId, string.Join(", ", routinePins.Intersect(channelPins)));
                continue; // Skip this channel due to GPIO conflict
            }
            
            // This channel is safe to use
            _logger.LogInformation("✅ Assigning routine to channel {ChannelId} - no GPIO conflicts", channelId);
            return channelId;
        }
        
        // 3. All channels have conflicts - find the one with minimum load as last resort
        // This should ideally not happen if channels are managed correctly
        _logger.LogWarning("⚠️  All channels have conflicts - using channel with minimum load");
        var channelLoads = new List<(int channelId, int load)>();
        for (int channelId = ActuatorConstants.Channels.MinChannelId; channelId <= ActuatorConstants.Channels.MaxChannelId; channelId++)
        {
            channelLoads.Add((channelId, GetChannelLoad(channelId)));
        }
        
        return channelLoads.OrderBy(cl => cl.load).First().channelId;
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

    private async Task<HashSet<string>> GetRoutinePinsAsync(RoutineCommandDto routine)
    {
        // Get all GPIO pins that this routine will use
        var pins = new HashSet<string>();
        foreach (var step in routine.Steps)
        {
            var actuator = await _actuatorRepository.GetByIdAsync(step.Actuator);
            if (actuator != null)
            {
                pins.Add(actuator.Pin.ToString());
            }
        }
        return pins;
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