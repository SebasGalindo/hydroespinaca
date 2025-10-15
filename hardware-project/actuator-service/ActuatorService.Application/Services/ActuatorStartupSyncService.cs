using ActuatorService.Domain.Interfaces;
using HydroEspinaca.Shared.Constants;
using HydroEspinaca.Shared.DTOs.Actuator;
using HydroEspinaca.Shared.Enums;
using Microsoft.Extensions.Logging;

namespace ActuatorService.Application.Services;

/// <summary>
/// Service responsible for synchronizing actuator states on startup.
/// Sends global shutdown command via MQTT and resets the state machine.
/// </summary>
public class ActuatorStartupSyncService
{
    private readonly IActuatorStateMachine _stateMachine;
    private readonly IActuatorRepository _actuatorRepository;
    private readonly IRoutineCommandPublisher _routineCommandPublisher;
    private readonly ILogger<ActuatorStartupSyncService> _logger;

    public ActuatorStartupSyncService(
        IActuatorStateMachine stateMachine,
        IActuatorRepository actuatorRepository,
        IRoutineCommandPublisher routineCommandPublisher,
        ILogger<ActuatorStartupSyncService> logger)
    {
        _stateMachine = stateMachine;
        _actuatorRepository = actuatorRepository;
        _routineCommandPublisher = routineCommandPublisher;
        _logger = logger;
    }

    /// <summary>
    /// Executes the startup synchronization routine:
    /// 1. Resets all in-memory states
    /// 2. Loads all actuators from database
    /// 3. Initializes state machine with actuator configurations
    /// 4. Sends global shutdown command via MQTT
    /// </summary>
    public async Task SynchronizeOnStartupAsync()
    {
        try
        {
            _logger.LogInformation("🚀 Starting actuator synchronization on startup...");

            // Step 1: Reset state machine
            _stateMachine.ResetAll();
            _logger.LogInformation("✅ State machine reset completed");

            // Step 2: Load all actuators from database
            var actuators = await _actuatorRepository.GetAllAsync();
            _logger.LogInformation("📦 Loaded {Count} actuators from database", actuators.Count);

            // Step 3: Initialize state machine with actuator configurations
            foreach (var actuator in actuators)
            {
                _stateMachine.InitializeActuator(
                    actuator.Id,
                    actuator.Esp32Id,
                    actuator.Pin,
                    actuator.Mode
                );
            }
            _logger.LogInformation("✅ Initialized {Count} actuators in state machine", actuators.Count);

            // Step 4: Group actuators by ESP32 and send shutdown commands
            var esp32Groups = actuators.GroupBy(a => a.Esp32Id);

            foreach (var esp32Group in esp32Groups)
            {
                await SendGlobalShutdownAsync(esp32Group.Key, esp32Group.ToList());
            }

            _logger.LogInformation("🎉 Actuator startup synchronization completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to synchronize actuators on startup");
            throw;
        }
    }

    /// <summary>
    /// Sends a global shutdown command to all actuators on a specific ESP32.
    /// </summary>
    private async Task SendGlobalShutdownAsync(string esp32Id, List<Domain.Entities.Actuator> actuators)
    {
        _logger.LogInformation("📡 Sending global shutdown to ESP32: {Esp32Id}", esp32Id);

        var jobSchedule = new JobScheduleDto
        {
            Esp32Id = esp32Id,
            Queue = new List<JobRoutineDto>
            {
                new JobRoutineDto
                {
                    CommandId = $"startup-shutdown-{DateTime.UtcNow:yyyyMMddHHmmss}",
                    BaseId = "system-init",
                    Steps = actuators.Select(a => new JobStepDto
                    {
                        Pin = a.Pin,
                        Mode = a.Mode.ToString(),
                        Power = PowerState.OFF.ToString(),
                        Duration = 0.1 // Minimal duration to ensure command is processed
                    }).ToList()
                }
            }
        };

        await _routineCommandPublisher.PublishJobScheduleAsync(jobSchedule);

        _logger.LogInformation("✅ Global shutdown command sent to ESP32: {Esp32Id} ({Count} actuators)",
            esp32Id, actuators.Count);
    }
}
