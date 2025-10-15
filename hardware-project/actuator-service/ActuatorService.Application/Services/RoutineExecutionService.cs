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
/// Routines execute concurrently when pins are available (max 1 running + 1 pending per pin).
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
        var rejectedRoutines = new List<string>();
        var routinesToPublish = new List<JobRoutineDto>();

        foreach (var resolvedRoutine in resolvedRoutines)
        {
            var commandId = GenerateCommandId(resolvedRoutine.RoutineId);
            var pins = resolvedRoutine.ResolvedSteps.Select(s => s.Pin).Distinct().ToList();
            var actuatorIds = resolvedRoutine.ResolvedSteps.Select(s => s.ActuatorId).Distinct().ToList();

            // Check if any pin already has 1 running + 1 pending (maximum allowed)
            var rejectRoutine = false;
            foreach (var pin in pins)
            {
                var hasRunning = _activeRoutines.Values.Any(r => r.Pins.Contains(pin) && r.Esp32Id == esp32Id);
                var hasPending = _pendingRoutines.Values.Any(r => r.Pins.Contains(pin) && r.Esp32Id == esp32Id);

                if (hasRunning && hasPending)
                {
                    // Reject: pin already has 1 running + 1 pending (max limit reached)
                    _logger.LogWarning("❌ Rejected routine {CommandId} - pin {Pin} already has 1 running + 1 pending routine (max limit)",
                        commandId, pin);
                    rejectedRoutines.Add($"{resolvedRoutine.RoutineId} (pin {pin})");
                    rejectRoutine = true;
                    break;
                }
            }

            if (rejectRoutine)
            {
                continue;
            }

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
                // Pins locked - add to pending queue (only if not already at limit)
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

        _logger.LogInformation("📊 Scheduling complete: {Active} active, {Pending} pending, {Rejected} rejected",
            _activeRoutines.Count, _pendingRoutines.Count, rejectedRoutines.Count);

        if (rejectedRoutines.Any())
        {
            _logger.LogWarning("⚠️ Rejected routines due to queue limit (1 running + 1 pending max): {RejectedRoutines}",
                string.Join(", ", rejectedRoutines));
        }

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
            Queue = allRoutines.Select(r => new QueuedCommandDto
            {
                CommandId = r.CommandId,
                Status = r.Status
            }).ToList()
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
            Queue = routines
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
        var actuatorCodeResolver = scope.ServiceProvider.GetRequiredService<IActuatorCodeResolver>();
        var actuatorRepository = scope.ServiceProvider.GetRequiredService<IActuatorRepository>();
        var commandExecutionService = scope.ServiceProvider.GetRequiredService<ICommandExecutionService>();

        // Get the SystemReset routine from database
        var systemResetRoutine = await internalRoutineRepository.GetByNameAsync("SystemReset");

        if (systemResetRoutine == null)
        {
            _logger.LogError("❌ SystemReset routine not found in database. Run seed data first.");
            throw new InvalidOperationException("SystemReset routine not found. Please run data seeding.");
        }

        _logger.LogInformation("📋 Found SystemReset routine with {StepCount} steps", systemResetRoutine.Steps.Count);

        // Get all actuators for filtering
        var allActuators = await actuatorRepository.GetAllAsync();

        // Resolve and filter steps
        var resolvedCommands = new List<ResolvedCommandDto>();

        foreach (var step in systemResetRoutine.Steps)
        {
            try
            {
                // Resolve actuator by code (OutputVariable now stores ActuatorCode)
                var actuator = await actuatorCodeResolver.ResolveAsync(step.OutputVariable);

                if (actuator == null)
                {
                    _logger.LogWarning("⚠️ Actuator {ActuatorCode} not found, skipping step", step.OutputVariable);
                    continue;
                }

                // Filter out inactive actuators
                if (actuator.Status != ActuatorStatus.Active)
                {
                    _logger.LogDebug("⏭️ Skipping actuator {ActuatorCode} ({ActuatorId}) - Status: {Status}",
                        actuator.Code, actuator.Id, actuator.Status);
                    continue;
                }

                // Filter by ESP32 if specified
                if (esp32Id != null && actuator.Esp32Id != esp32Id)
                {
                    continue;
                }

                resolvedCommands.Add(new ResolvedCommandDto
                {
                    ActuatorCode = step.OutputVariable,
                    ActuatorId = actuator.Id,
                    Esp32Id = actuator.Esp32Id,
                    Pin = actuator.Pin,
                    Mode = actuator.Mode,
                    Power = step.Power,
                    DutyCycle = step.DutyCycle,
                    Duration = step.Duration
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error resolving actuator {ActuatorCode} for SystemReset",
                    step.OutputVariable);
            }
        }

        if (esp32Id != null)
        {
            _logger.LogInformation("🔍 Filtered to {CommandCount} active commands for ESP32 {Esp32Id}",
                resolvedCommands.Count, esp32Id);
        }
        else
        {
            _logger.LogInformation("🔍 Filtered to {CommandCount} active commands (from {TotalSteps} total)",
                resolvedCommands.Count, systemResetRoutine.Steps.Count);
        }

        if (resolvedCommands.Count == 0)
        {
            _logger.LogWarning("⚠️ No actuators to reset");
            return;
        }

        // Group commands by ESP32
        var commandsByEsp32 = resolvedCommands.GroupBy(c => c.Esp32Id);

        foreach (var esp32Group in commandsByEsp32)
        {
            var esp32Commands = esp32Group.ToList();
            _logger.LogInformation("🔌 Resetting {Count} actuators for ESP32 {Esp32Id}",
                esp32Commands.Count, esp32Group.Key);

            // Send immediate reset commands (bypass queue, no pin locking)
            await commandExecutionService.SendImmediateResetCommandsAsync(esp32Commands, esp32Group.Key);

            _logger.LogInformation("📤 Published {CommandCount} immediate SystemReset commands for ESP32 {Esp32Id}",
                esp32Commands.Count, esp32Group.Key);
        }

        _logger.LogInformation("🏁 Reset complete: {Count} actuators scheduled to OFF", resolvedCommands.Count);
    }

    private static string GenerateCommandId(string routineId)
    {
        return $"{routineId}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
    }
}
