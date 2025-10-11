using System.Collections.Concurrent;
using ActuatorService.Application.DTOs;
using ActuatorService.Application.Interfaces;
using ActuatorService.Application.Models;
using ActuatorService.Domain.Entities;
using ActuatorService.Domain.Interfaces;
using HydroEspinaca.Shared.Constants;
using HydroEspinaca.Shared.DTOs.Actuator;
using HydroEspinaca.Shared.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ActuatorService.Application.Services;

/// <summary>
/// Manages routine execution with dynamic pin-based locking.
/// No fixed channel limits - routines execute concurrently when pins are available.
/// </summary>
public class RoutineExecutionService : IRoutineExecutionService
{
    private readonly ConcurrentDictionary<string, ActiveRoutine> _activeRoutines = new();
    private readonly ConcurrentDictionary<string, PendingRoutine> _pendingRoutines = new();
    private readonly IPinLockRegistry _pinLockRegistry;
    private readonly IServiceProvider _serviceProvider;
    private readonly IActuatorStateMachine _stateMachine;
    private readonly ILogger<RoutineExecutionService> _logger;

    public RoutineExecutionService(
        IPinLockRegistry pinLockRegistry,
        IServiceProvider serviceProvider,
        IActuatorStateMachine stateMachine,
        ILogger<RoutineExecutionService> logger)
    {
        _pinLockRegistry = pinLockRegistry;
        _serviceProvider = serviceProvider;
        _stateMachine = stateMachine;
        _logger = logger;
    }

    public async Task<List<string>> ScheduleRoutinesAsync(List<ResolvedRoutineDto> resolvedRoutines, string esp32Id)
    {
        _logger.LogInformation("📋 Scheduling {Count} routines for ESP32 {Esp32Id}",
            resolvedRoutines.Count, esp32Id);

        var scheduledCommandIds = new List<string>();
        var routinesToPublish = new List<JobRoutineDto>();

        foreach (var resolvedRoutine in resolvedRoutines)
        {
            var commandId = GenerateCommandId(resolvedRoutine.RoutineId);
            var pins = resolvedRoutine.ResolvedSteps.Select(s => s.Pin).Distinct().ToList();
            var actuatorIds = resolvedRoutine.ResolvedSteps.Select(s => s.ActuatorId).Distinct().ToList();

            var stepMappings = resolvedRoutine.ResolvedSteps.Select(step => new RoutineStepMapping
            {
                Pin = step.Pin,
                ActuatorId = step.ActuatorId,
                OutputVariableId = step.OutputVariableId,
                Duration = step.Duration,
                DutyCycle = step.DutyCycle,
                Power = step.Power
            }).ToList();

            var jobRoutine = CreateJobRoutineDto(resolvedRoutine, commandId);

            // Try to acquire pin locks
            if (_pinLockRegistry.TryLock(pins, commandId))
            {
                // Pins available - activate immediately
                var activeRoutine = new ActiveRoutine
                {
                    CommandId = commandId,
                    RoutineId = resolvedRoutine.RoutineId,
                    Esp32Id = esp32Id,
                    Pins = pins,
                    ActuatorIds = actuatorIds,
                    StartTime = DateTime.UtcNow,
                    StepMappings = stepMappings
                };

                _activeRoutines.TryAdd(commandId, activeRoutine);
                routinesToPublish.Add(jobRoutine);

                // Update actuator states to ON with duration and duty cycle
                UpdateActuatorStates(stepMappings, PowerState.ON, commandId);

                _logger.LogInformation("✅ Activated routine {CommandId} immediately (pins available: {Pins})",
                    commandId, string.Join(", ", pins));
            }
            else
            {
                // Pins locked - add to pending queue
                var pendingRoutine = new PendingRoutine
                {
                    CommandId = commandId,
                    RoutineId = resolvedRoutine.RoutineId,
                    Esp32Id = esp32Id,
                    Pins = pins,
                    ActuatorIds = actuatorIds,
                    ScheduledTime = DateTime.UtcNow,
                    StepMappings = stepMappings,
                    JobRoutineDto = jobRoutine
                };

                _pendingRoutines.TryAdd(commandId, pendingRoutine);

                _logger.LogInformation("⏸️ Routine {CommandId} pending (waiting for pins: {Pins})",
                    commandId, string.Join(", ", pins));
            }

            scheduledCommandIds.Add(commandId);
        }

        // Publish activated routines to MQTT
        if (routinesToPublish.Any())
        {
            await PublishJobScheduleAsync(esp32Id, routinesToPublish);
        }

        _logger.LogInformation("📊 Scheduling complete: {Active} active, {Pending} pending",
            _activeRoutines.Count, _pendingRoutines.Count);

        return scheduledCommandIds;
    }

    public async Task OnRoutineCompletedAsync(string commandId)
    {
        _logger.LogInformation("🏁 Processing completion for routine {CommandId}", commandId);

        if (!_activeRoutines.TryRemove(commandId, out var completedRoutine))
        {
            _logger.LogWarning("⚠️ Completed routine {CommandId} not found in active routines", commandId);
            return;
        }

        // Release pin locks
        _pinLockRegistry.Release(completedRoutine.Pins);

        // Update actuator states to OFF (no duration for OFF state)
        UpdateActuatorStates(completedRoutine.StepMappings, PowerState.OFF, commandId);

        _logger.LogInformation("🔓 Released {Count} pins for completed routine {CommandId}: {Pins}",
            completedRoutine.Pins.Count, commandId, string.Join(", ", completedRoutine.Pins));

        // Try to activate pending routines that were waiting for these pins
        await ActivatePendingRoutinesAsync(completedRoutine.Esp32Id);
    }

    public Task<JobStatusDto> GetStatusAsync(string? esp32Id = null)
    {
        var activeRoutines = esp32Id == null
            ? _activeRoutines.Values.ToList()
            : _activeRoutines.Values.Where(r => r.Esp32Id == esp32Id).ToList();

        var pendingRoutines = esp32Id == null
            ? _pendingRoutines.Values.ToList()
            : _pendingRoutines.Values.Where(r => r.Esp32Id == esp32Id).ToList();

        var allRoutines = activeRoutines
            .Select(r => new { r.CommandId, Status = "IN_PROGRESS" })
            .Concat(pendingRoutines.Select(r => new { r.CommandId, Status = "SCHEDULED" }))
            .ToList();

        var status = new JobStatusDto
        {
            Esp32Id = esp32Id ?? "all",
            Channels = new List<ChannelStatusDto>
            {
                new ChannelStatusDto
                {
                    Channel = 0, // Single virtual channel representing all routines
                    Queue = allRoutines.Select(r => new QueuedCommandDto
                    {
                        CommandId = r.CommandId,
                        Status = r.Status
                    }).ToList()
                }
            }
        };

        return Task.FromResult(status);
    }

    public Task<RoutineExecutionStats> GetStatsAsync()
    {
        var stats = new RoutineExecutionStats
        {
            ActiveCount = _activeRoutines.Count,
            PendingCount = _pendingRoutines.Count,
            TotalLockedPins = _pinLockRegistry.GetAllLocks().Count,
            Esp32Ids = _activeRoutines.Values.Select(r => r.Esp32Id)
                .Concat(_pendingRoutines.Values.Select(r => r.Esp32Id))
                .Distinct()
                .ToList()
        };

        return Task.FromResult(stats);
    }

    public Task ClearAsync(string? esp32Id = null)
    {
        if (esp32Id == null)
        {
            // Clear all
            var allPins = _activeRoutines.Values.SelectMany(r => r.Pins).Distinct();
            _pinLockRegistry.Release(allPins);
            _activeRoutines.Clear();
            _pendingRoutines.Clear();

            _logger.LogInformation("🧹 Cleared all routines and pin locks");
        }
        else
        {
            // Clear for specific ESP32
            var activeToRemove = _activeRoutines.Where(kvp => kvp.Value.Esp32Id == esp32Id).ToList();
            var pendingToRemove = _pendingRoutines.Where(kvp => kvp.Value.Esp32Id == esp32Id).ToList();

            foreach (var (commandId, routine) in activeToRemove)
            {
                _pinLockRegistry.Release(routine.Pins);
                _activeRoutines.TryRemove(commandId, out _);
            }

            foreach (var (commandId, _) in pendingToRemove)
            {
                _pendingRoutines.TryRemove(commandId, out _);
            }

            _logger.LogInformation("🧹 Cleared {ActiveCount} active and {PendingCount} pending routines for ESP32 {Esp32Id}",
                activeToRemove.Count, pendingToRemove.Count, esp32Id);
        }

        return Task.CompletedTask;
    }

    private async Task ActivatePendingRoutinesAsync(string esp32Id)
    {
        var pendingForEsp32 = _pendingRoutines.Values
            .Where(r => r.Esp32Id == esp32Id)
            .OrderBy(r => r.ScheduledTime)
            .ToList();

        if (!pendingForEsp32.Any())
        {
            _logger.LogDebug("No pending routines to activate for ESP32 {Esp32Id}", esp32Id);
            return;
        }

        var activated = new List<JobRoutineDto>();

        foreach (var pending in pendingForEsp32)
        {
            // Try to acquire locks
            if (_pinLockRegistry.TryLock(pending.Pins, pending.CommandId))
            {
                // Remove from pending
                if (!_pendingRoutines.TryRemove(pending.CommandId, out _))
                {
                    // Someone else activated it - release locks and continue
                    _pinLockRegistry.Release(pending.Pins);
                    continue;
                }

                // Add to active
                var activeRoutine = new ActiveRoutine
                {
                    CommandId = pending.CommandId,
                    RoutineId = pending.RoutineId,
                    Esp32Id = pending.Esp32Id,
                    Pins = pending.Pins,
                    ActuatorIds = pending.ActuatorIds,
                    StartTime = DateTime.UtcNow,
                    StepMappings = pending.StepMappings
                };

                _activeRoutines.TryAdd(pending.CommandId, activeRoutine);
                activated.Add(pending.JobRoutineDto);

                // Update actuator states to ON with duration and duty cycle
                UpdateActuatorStates(pending.StepMappings, PowerState.ON, pending.CommandId);

                _logger.LogInformation("🚀 Activated pending routine {CommandId} (pins now available: {Pins})",
                    pending.CommandId, string.Join(", ", pending.Pins));
            }
        }

        // Publish newly activated routines
        if (activated.Any())
        {
            await PublishJobScheduleAsync(esp32Id, activated);
            _logger.LogInformation("📤 Published {Count} newly activated routines to MQTT", activated.Count);
        }
    }

    private JobRoutineDto CreateJobRoutineDto(ResolvedRoutineDto resolvedRoutine, string commandId)
    {
        return new JobRoutineDto
        {
            CommandId = commandId,
            BaseId = resolvedRoutine.RoutineId,
            Steps = resolvedRoutine.ResolvedSteps.Select(step => new JobStepDto
            {
                Pin = step.Pin,
                Mode = step.Mode.ToString(),
                Power = step.Power,
                DutyCycle = step.DutyCycle,
                Duration = step.Duration
            }).ToList()
        };
    }

    private async Task PublishJobScheduleAsync(string esp32Id, List<JobRoutineDto> routines)
    {
        using var scope = _serviceProvider.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IRoutineCommandPublisher>();

        var jobSchedule = new JobScheduleDto
        {
            Esp32Id = esp32Id,
            JobSchedule = new List<JobChannelDto>
            {
                new JobChannelDto
                {
                    Channel = 0, // Single virtual channel
                    Queue = routines
                }
            }
        };

        await publisher.PublishJobScheduleAsync(jobSchedule);
    }

    private void UpdateActuatorStates(List<RoutineStepMapping> stepMappings, PowerState state, string commandId)
    {
        // Group steps by ActuatorId to calculate total duration and aggregate dutyCycle
        var actuatorData = stepMappings
            .GroupBy(m => m.ActuatorId)
            .Select(g => new
            {
                ActuatorId = g.Key,
                // Sum durations for all steps of this actuator
                TotalDuration = g.Sum(m => m.Duration),
                // Use maximum dutyCycle if multiple steps (for PWM actuators)
                DutyCycle = g.Max(m => m.DutyCycle),
                // Use the Power state from the first step (should be consistent)
                Power = g.First().Power
            })
            .ToList();

        foreach (var data in actuatorData)
        {
            if (state == PowerState.ON)
            {
                // For ON state, pass the total duration and dutyCycle
                _stateMachine.UpdateState(
                    data.ActuatorId,
                    state,
                    duration: data.TotalDuration,
                    dutyCycle: data.DutyCycle,
                    commandId: commandId
                );

                _logger.LogDebug("🔄 Updated actuator {ActuatorId} state to ON: duration={Duration}s, dutyCycle={DutyCycle}%",
                    data.ActuatorId, data.TotalDuration, data.DutyCycle);
            }
            else
            {
                // For OFF state, clear duration and dutyCycle
                _stateMachine.UpdateState(
                    data.ActuatorId,
                    state,
                    duration: null,
                    dutyCycle: null,
                    commandId: commandId
                );

                _logger.LogDebug("🔄 Updated actuator {ActuatorId} state to OFF",
                    data.ActuatorId);
            }
        }
    }

    public async Task ResetAllActuatorsAsync(string? esp32Id = null)
    {
        _logger.LogInformation("🔄 Resetting all actuators{Esp32Filter}",
            esp32Id != null ? $" for ESP32 {esp32Id}" : "");

        using var scope = _serviceProvider.CreateScope();
        var internalRoutineRepository = scope.ServiceProvider.GetRequiredService<IInternalRoutineRepository>();
        var routineValidationService = scope.ServiceProvider.GetRequiredService<IRoutineValidationService>();
        var actuatorRepository = scope.ServiceProvider.GetRequiredService<IActuatorRepository>();

        // Get the SystemReset routine from database
        var systemResetRoutine = await internalRoutineRepository.GetByNameAsync("SystemReset");

        if (systemResetRoutine == null)
        {
            _logger.LogError("❌ SystemReset routine not found in database. Run seed data first.");
            throw new InvalidOperationException("SystemReset routine not found. Please run data seeding.");
        }

        _logger.LogInformation("📋 Found SystemReset routine with {StepCount} steps", systemResetRoutine.Steps.Count);

        // Get all actuators and control outputs for filtering
        var controlOutputRepository = scope.ServiceProvider.GetRequiredService<IControlOutputRepository>();
        var allControlOutputs = await controlOutputRepository.GetAllAsync();
        var allActuators = await actuatorRepository.GetAllAsync();

        // Create a map of OutputVariable -> Actuator for quick lookup
        var outputVarToActuator = allControlOutputs
            .Join(allActuators,
                co => co.ActuatorId,
                a => a.Id,
                (co, a) => new { OutputVariableId = co.Id, Actuator = a })
            .ToDictionary(x => x.OutputVariableId, x => x.Actuator);

        // Filter steps: only include ACTIVE actuators and optionally by ESP32
        var stepsToExecute = systemResetRoutine.Steps
            .Where(step =>
            {
                if (!outputVarToActuator.TryGetValue(step.OutputVariable, out var actuator))
                {
                    _logger.LogWarning("⚠️ OutputVariable {OutputVariableId} not found, skipping step", step.OutputVariable);
                    return false;
                }

                // Filter out inactive actuators
                if (actuator.Status != ActuatorStatus.Active)
                {
                    _logger.LogDebug("⏭️ Skipping actuator {ActuatorCode} ({ActuatorId}) - Status: {Status}",
                        actuator.Code, actuator.Id, actuator.Status);
                    return false;
                }

                // Filter by ESP32 if specified
                if (esp32Id != null && actuator.Esp32Id != esp32Id)
                {
                    return false;
                }

                return true;
            })
            .ToList();

        if (esp32Id != null)
        {
            _logger.LogInformation("🔍 Filtered to {StepCount} active steps for ESP32 {Esp32Id}",
                stepsToExecute.Count, esp32Id);
        }
        else
        {
            _logger.LogInformation("🔍 Filtered to {StepCount} active steps (from {TotalSteps} total)",
                stepsToExecute.Count, systemResetRoutine.Steps.Count);
        }

        // Convert InternalRoutineStep to RoutineStepDto
        var steps = stepsToExecute.Select(step => new RoutineStepDto
        {
            OutputVariable = step.OutputVariable,
            Power = step.Power,
            Duration = step.Duration,
            DutyCycle = step.DutyCycle
        }).ToList();

        // Validate and resolve steps (OutputVariable -> ControlOutput -> Actuator)
        var resolvedSteps = await routineValidationService.ValidateAndResolveStepsAsync(steps);

        // Group by ESP32
        var stepsByEsp32 = resolvedSteps.GroupBy(s => s.Esp32Id);

        foreach (var esp32Group in stepsByEsp32)
        {
            var esp32Steps = esp32Group.ToList();
            _logger.LogInformation("🔌 Resetting {Count} actuators for ESP32 {Esp32Id}",
                esp32Steps.Count, esp32Group.Key);

            var resolvedRoutine = new ResolvedRoutineDto
            {
                RoutineId = "system_reset",
                Esp32Id = esp32Group.Key,
                ResolvedSteps = esp32Steps
            };

            // Schedule using the same execution service (will publish via MQTT)
            var commandIds = await ScheduleRoutinesAsync(
                new List<ResolvedRoutineDto> { resolvedRoutine },
                esp32Group.Key
            );

            _logger.LogInformation("📤 Published SystemReset routine for ESP32 {Esp32Id} with {StepCount} steps (CommandId: {CommandId})",
                esp32Group.Key, esp32Steps.Count, commandIds.First());
        }

        _logger.LogInformation("🏁 Reset complete: {Count} actuators will be set to OFF", resolvedSteps.Count);
    }

    private static string GenerateCommandId(string routineId)
    {
        return $"{routineId}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
    }
}
