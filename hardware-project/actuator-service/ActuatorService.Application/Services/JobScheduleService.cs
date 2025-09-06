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

        // Find the best channel for this routine (simplified for now - round robin)
        var channelId = FindBestChannelForRoutine(routine, priority);

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

    private int FindBestChannelForRoutine(RoutineCommandDto routine, RoutinePriority priority)
    {
        var routineActuators = routine.Steps.Select(s => s.Actuator).ToHashSet();
        
        // 1. Try to find channels with same actuators (for consistency)
        for (int channelId = ActuatorConstants.Channels.MinChannelId; channelId <= ActuatorConstants.Channels.MaxChannelId; channelId++)
        {
            var channelActuators = GetChannelActuators(channelId);
            if (channelActuators.Any() && routineActuators.Overlaps(channelActuators))
            {
                return channelId;
            }
        }
        
        // 2. Try to find free channels
        for (int channelId = ActuatorConstants.Channels.MinChannelId; channelId <= ActuatorConstants.Channels.MaxChannelId; channelId++)
        {
            if (IsChannelFree(channelId))
            {
                return channelId;
            }
        }
        
        // 3. Find channel with minimum load
        var channelLoads = new List<(int channelId, int load)>();
        for (int channelId = ActuatorConstants.Channels.MinChannelId; channelId <= ActuatorConstants.Channels.MaxChannelId; channelId++)
        {
            channelLoads.Add((channelId, GetChannelLoad(channelId)));
        }
        
        return channelLoads.OrderBy(cl => cl.load).First().channelId;
    }
    
    private HashSet<string> GetChannelActuators(int channelId)
    {
        // Get all actuators currently in this channel across all ESP32s
        var actuators = new HashSet<string>();
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
                        actuators.Add(step.Pin); // Using pin as unique identifier
                    }
                }
            }
        }
        return actuators;
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