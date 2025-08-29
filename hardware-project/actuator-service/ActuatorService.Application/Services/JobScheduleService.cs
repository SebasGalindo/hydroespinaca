using ActuatorService.Application.Interfaces;
using ActuatorService.Domain.Interfaces;
using HydroEspinaca.Shared.DTOs.Actuator;
using Microsoft.Extensions.Logging;

namespace ActuatorService.Application.Services;

public class JobScheduleService : IJobScheduleService
{
    private const int NUM_CHANNELS = 3;
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
        // Get the first actuator ID from the routines to determine ESP32
        var firstActuatorId = routines.SelectMany(r => r.Steps).Select(s => s.Actuator).FirstOrDefault();
        
        if (firstActuatorId != null)
        {
            var actuator = await _actuatorRepository.GetByIdAsync(firstActuatorId);
            if (actuator?.Esp32Id != null)
            {
                return actuator.Esp32Id;
            }
        }
        
        // Default ESP32 ID if we can't determine it
        return "default-esp32";
    }

    private async Task AssignRoutineToChannelAsync(string esp32Id, RoutineCommandDto routine)
    {
        var commandId = GenerateCommandId(routine.RoutineId);
        
        // Enrich steps with actuator data
        var enrichedSteps = await EnrichRoutineStepsAsync(routine.Steps);
        
        var jobRoutine = new JobRoutineState
        {
            CommandId = commandId,
            Steps = enrichedSteps,
            Status = "scheduled"
        };

        // Determine priority level
        var priority = DeterminePriority(routine);
        
        // Find the best channel for this routine (simplified for now - round robin)
        var channelId = FindBestChannelForRoutine(routine, priority);
        
        // Add to the selected channel via state manager
        _stateManager.AddRoutineToSchedule(esp32Id, jobRoutine, channelId);

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

    private RoutinePriority DeterminePriority(RoutineCommandDto routine)
    {
        // Control routines: all steps have "OFF" power or 0 duty cycle
        bool isControl = routine.Steps.All(step => 
            (step.Power == "OFF") || (step.DutyCycle == 0));
            
        if (isControl)
            return RoutinePriority.Control;

        // Single step routines get medium-high priority
        if (routine.Steps.Count == 1)
            return RoutinePriority.SingleStep;

        return RoutinePriority.Normal;
    }

    private int FindBestChannelForRoutine(RoutineCommandDto routine, RoutinePriority priority)
    {
        // Simplified channel assignment - round robin based on routine hash
        var routineHash = routine.RoutineId.GetHashCode();
        return (Math.Abs(routineHash) % NUM_CHANNELS) + 1;
    }
}

internal enum RoutinePriority
{
    Normal = 0,
    SingleStep = 1,
    Control = 2
}