using System.Collections.Concurrent;
using ActuatorService.Application.DTOs;
using ActuatorService.Application.Helpers;
using ActuatorService.Application.Interfaces;
using ActuatorService.Domain.Entities;
using ActuatorService.Domain.Interfaces;
using HydroEspinaca.Shared.Constants;
using HydroEspinaca.Shared.DTOs.Actuator;
using HydroEspinaca.Shared.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ActuatorService.Application.Services;

/// <summary>
/// Simplified command execution service
/// Handles individual commands without ControlOutputs dependency
/// </summary>
public interface ICommandExecutionService
{
    Task<List<string>> ScheduleCommandsAsync(List<ResolvedCommandDto> commands, string esp32Id);
    Task OnCommandCompletedAsync(string commandId);
    Task<JobStatusDto> GetStatusAsync(string? esp32Id = null);
    Task<RoutineExecutionStats> GetStatsAsync();
    Task ClearAsync(string? esp32Id = null);
    Task SendImmediateResetCommandsAsync(List<ResolvedCommandDto> commands, string esp32Id);
    Task ResetAllActuatorsAsync(string? esp32Id = null);
}

public class CommandExecutionService : ICommandExecutionService
{
    private readonly ConcurrentDictionary<string, ActiveCommand> _activeCommands = new();
    private readonly ConcurrentDictionary<string, PendingCommand> _pendingCommands = new();
    private readonly IPinLockRegistry _pinLockRegistry;
    private readonly IActuatorStateMachine _stateMachine;
    private readonly IRoutineCommandPublisher _mqttPublisher;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CommandExecutionService> _logger;

    public CommandExecutionService(
        IPinLockRegistry pinLockRegistry,
        IActuatorStateMachine stateMachine,
        IRoutineCommandPublisher mqttPublisher,
        IServiceScopeFactory scopeFactory,
        ILogger<CommandExecutionService> logger)
    {
        _pinLockRegistry = pinLockRegistry;
        _stateMachine = stateMachine;
        _mqttPublisher = mqttPublisher;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<List<string>> ScheduleCommandsAsync(List<ResolvedCommandDto> commands, string esp32Id)
    {
        _logger.LogInformation("📋 Scheduling {Count} commands for ESP32 {Esp32Id}", commands.Count, esp32Id);

        var scheduledCommandIds = new List<string>();
        var rejectedCommands = new List<string>();
        var commandsToPublish = new List<JobRoutineDto>();

        foreach (var command in commands)
        {
            var commandId = CommandIdHelper.GenerateCommandId(command.ActuatorCode);
            var pins = new List<string> { command.Pin };

            // Check if pin already has 1 running + 1 pending (maximum allowed)
            var hasRunning = _activeCommands.Values.Any(c => c.Pin == command.Pin && c.Esp32Id == esp32Id);
            var hasPending = _pendingCommands.Values.Any(c => c.Pin == command.Pin && c.Esp32Id == esp32Id);

            if (hasRunning && hasPending)
            {
                // Reject: pin already has 1 running + 1 pending (max limit reached)
                _logger.LogWarning("❌ Rejected command {CommandId} for {ActuatorCode} - pin {Pin} already has 1 running + 1 pending command (max limit)",
                    commandId, command.ActuatorCode, command.Pin);
                rejectedCommands.Add($"{command.ActuatorCode} (pin {command.Pin})");
                continue;
            }

            // Try to acquire pin lock
            if (_pinLockRegistry.TryLock(pins, commandId))
            {
                // Pin available - activate immediately
                var activeCommand = new ActiveCommand
                {
                    CommandId = commandId,
                    ActuatorCode = command.ActuatorCode,
                    ActuatorId = command.ActuatorId,
                    Esp32Id = esp32Id,
                    Pin = command.Pin,
                    StartTime = DateTime.UtcNow,
                    Power = command.Power,
                    DutyCycle = command.DutyCycle,
                    Duration = command.Duration
                };

                _activeCommands.TryAdd(commandId, activeCommand);

                // Create MQTT payload
                var jobRoutine = CreateJobRoutineDto(command, commandId);
                commandsToPublish.Add(jobRoutine);

                // Update actuator state to ON
                _stateMachine.UpdateState(command.ActuatorId, PowerState.ON, command.Duration, command.DutyCycle, commandId);

                _logger.LogInformation("✅ Activated command {CommandId} for {ActuatorCode} (pin {Pin} available)",
                    commandId, command.ActuatorCode, command.Pin);
            }
            else
            {
                // Pin locked - add to pending queue (only if not already at limit)
                var pendingCommand = new PendingCommand
                {
                    CommandId = commandId,
                    ActuatorCode = command.ActuatorCode,
                    ActuatorId = command.ActuatorId,
                    Esp32Id = esp32Id,
                    Pin = command.Pin,
                    ScheduledTime = DateTime.UtcNow,
                    Power = command.Power,
                    DutyCycle = command.DutyCycle,
                    Duration = command.Duration,
                    JobRoutineDto = CreateJobRoutineDto(command, commandId)
                };

                _pendingCommands.TryAdd(commandId, pendingCommand);

                _logger.LogInformation("⏸️ Command {CommandId} for {ActuatorCode} pending (waiting for pin {Pin})",
                    commandId, command.ActuatorCode, command.Pin);
            }

            scheduledCommandIds.Add(commandId);
        }

        // Publish activated commands to MQTT
        if (commandsToPublish.Any())
        {
            await PublishJobScheduleAsync(esp32Id, commandsToPublish);
        }

        _logger.LogInformation("📊 Scheduling complete: {Active} active, {Pending} pending, {Rejected} rejected",
            _activeCommands.Count, _pendingCommands.Count, rejectedCommands.Count);

        if (rejectedCommands.Any())
        {
            _logger.LogWarning("⚠️ Rejected commands due to queue limit (1 running + 1 pending max): {RejectedCommands}",
                string.Join(", ", rejectedCommands));
        }

        return scheduledCommandIds;
    }

    public async Task OnCommandCompletedAsync(string commandId)
    {
        _logger.LogInformation("🏁 Processing completion for command {CommandId}", commandId);

        if (!_activeCommands.TryRemove(commandId, out var completedCommand))
        {
            _logger.LogWarning("⚠️ Completed command {CommandId} not found in active commands", commandId);
            throw new InvalidOperationException($"Command {commandId} not found in active commands");
        }

        // Release pin lock
        _pinLockRegistry.Release(new List<string> { completedCommand.Pin });

        // Update actuator state to OFF
        _stateMachine.UpdateState(completedCommand.ActuatorId, PowerState.OFF, 0, null, commandId);

        _logger.LogInformation("🔓 Released pin {Pin} for completed command {CommandId}",
            completedCommand.Pin, commandId);

        // Try to activate pending commands that were waiting for this pin
        await ActivatePendingCommandsAsync(completedCommand.Esp32Id);
    }

    private async Task ActivatePendingCommandsAsync(string esp32Id)
    {
        var pendingForEsp32 = _pendingCommands.Values
            .Where(c => c.Esp32Id == esp32Id)
            .OrderBy(c => c.ScheduledTime)
            .ToList();

        if (!pendingForEsp32.Any())
        {
            _logger.LogDebug("No pending commands to activate for ESP32 {Esp32Id}", esp32Id);
            return;
        }

        var activated = new List<JobRoutineDto>();

        foreach (var pending in pendingForEsp32)
        {
            var pins = new List<string> { pending.Pin };

            // Try to acquire lock
            if (_pinLockRegistry.TryLock(pins, pending.CommandId))
            {
                // Remove from pending
                if (!_pendingCommands.TryRemove(pending.CommandId, out _))
                {
                    // Someone else activated it - release lock and continue
                    _pinLockRegistry.Release(pins);
                    continue;
                }

                // Add to active
                var activeCommand = new ActiveCommand
                {
                    CommandId = pending.CommandId,
                    ActuatorCode = pending.ActuatorCode,
                    ActuatorId = pending.ActuatorId,
                    Esp32Id = pending.Esp32Id,
                    Pin = pending.Pin,
                    StartTime = DateTime.UtcNow,
                    Power = pending.Power,
                    DutyCycle = pending.DutyCycle,
                    Duration = pending.Duration
                };

                _activeCommands.TryAdd(pending.CommandId, activeCommand);
                activated.Add(pending.JobRoutineDto);

                // Update actuator state to ON
                _stateMachine.UpdateState(pending.ActuatorId, PowerState.ON, pending.Duration, pending.DutyCycle, pending.CommandId);

                _logger.LogInformation("🚀 Activated pending command {CommandId} for {ActuatorCode} (pin {Pin} now available)",
                    pending.CommandId, pending.ActuatorCode, pending.Pin);
            }
        }

        // Publish newly activated commands
        if (activated.Any())
        {
            await PublishJobScheduleAsync(esp32Id, activated);
            _logger.LogInformation("📤 Published {Count} newly activated commands to MQTT", activated.Count);
        }
    }

    private JobRoutineDto CreateJobRoutineDto(ResolvedCommandDto command, string commandId)
    {
        return new JobRoutineDto
        {
            CommandId = commandId,
            BaseId = command.ActuatorCode,
            Steps = new List<JobStepDto>
            {
                new JobStepDto
                {
                    Pin = command.Pin,
                    Mode = command.Mode.ToString(),
                    Power = command.Power,
                    DutyCycle = command.DutyCycle,
                    Duration = command.Duration
                }
            }
        };
    }

    private async Task PublishJobScheduleAsync(string esp32Id, List<JobRoutineDto> jobRoutines)
    {
        var jobSchedule = new JobScheduleDto
        {
            Esp32Id = esp32Id,
            Queue = jobRoutines
        };

        await _mqttPublisher.PublishJobScheduleAsync(jobSchedule);
    }

    public Task<JobStatusDto> GetStatusAsync(string? esp32Id = null)
    {
        var activeCommands = esp32Id == null
            ? _activeCommands.Values.ToList()
            : _activeCommands.Values.Where(c => c.Esp32Id == esp32Id).ToList();

        var pendingCommands = esp32Id == null
            ? _pendingCommands.Values.ToList()
            : _pendingCommands.Values.Where(c => c.Esp32Id == esp32Id).ToList();

        // Combine active and pending commands
        var allCommands = activeCommands
            .Select(c => new QueuedCommandDto { CommandId = c.CommandId, Status = "running" })
            .Concat(pendingCommands.Select(c => new QueuedCommandDto { CommandId = c.CommandId, Status = "scheduled" }))
            .ToList();

        // Get ESP32 ID (use first available, or the filtered one)
        var targetEsp32Id = esp32Id ??
            _activeCommands.Values.Select(c => c.Esp32Id).Concat(_pendingCommands.Values.Select(c => c.Esp32Id))
            .FirstOrDefault() ?? "unknown";

        var status = new JobStatusDto
        {
            Esp32Id = targetEsp32Id,
            Queue = allCommands
        };

        return Task.FromResult(status);
    }

    public Task<RoutineExecutionStats> GetStatsAsync()
    {
        var stats = new RoutineExecutionStats
        {
            ActiveCount = _activeCommands.Count,
            PendingCount = _pendingCommands.Count,
            TotalLockedPins = _pinLockRegistry.GetAllLocks().Count,
            Esp32Ids = _activeCommands.Values.Select(c => c.Esp32Id)
                .Concat(_pendingCommands.Values.Select(c => c.Esp32Id))
                .Distinct()
                .ToList()
        };

        return Task.FromResult(stats);
    }

    public Task ClearAsync(string? esp32Id = null)
    {
        _logger.LogInformation("🧹 Clearing all commands{Esp32Filter}",
            esp32Id != null ? $" for ESP32 {esp32Id}" : "");

        // Get commands to clear
        var activeToRemove = esp32Id == null
            ? _activeCommands.Keys.ToList()
            : _activeCommands.Values.Where(c => c.Esp32Id == esp32Id).Select(c => c.CommandId).ToList();

        var pendingToRemove = esp32Id == null
            ? _pendingCommands.Keys.ToList()
            : _pendingCommands.Values.Where(c => c.Esp32Id == esp32Id).Select(c => c.CommandId).ToList();

        // Remove active commands and release their pins
        foreach (var commandId in activeToRemove)
        {
            if (_activeCommands.TryRemove(commandId, out var activeCommand))
            {
                _pinLockRegistry.Release(new List<string> { activeCommand.Pin });
                _stateMachine.UpdateState(activeCommand.ActuatorId, PowerState.OFF, 0, null, commandId);
                _logger.LogDebug("🗑️ Removed active command {CommandId}, released pin {Pin}, set actuator {ActuatorId} to OFF",
                    commandId, activeCommand.Pin, activeCommand.ActuatorId);
            }
        }

        // Remove pending commands
        foreach (var commandId in pendingToRemove)
        {
            if (_pendingCommands.TryRemove(commandId, out var pendingCommand))
            {
                _logger.LogDebug("🗑️ Removed pending command {CommandId}", commandId);
            }
        }

        _logger.LogInformation("✅ Cleared {ActiveCount} active and {PendingCount} pending commands",
            activeToRemove.Count, pendingToRemove.Count);

        return Task.CompletedTask;
    }

    public async Task SendImmediateResetCommandsAsync(List<ResolvedCommandDto> commands, string esp32Id)
    {
        _logger.LogInformation("⚡ Sending {Count} immediate reset commands for ESP32 {Esp32Id}",
            commands.Count, esp32Id);

        var jobRoutines = new List<JobRoutineDto>();

        foreach (var command in commands)
        {
            var commandId = CommandIdHelper.GenerateCommandId($"reset_{command.ActuatorCode}");

            // Register command in active commands for MQTT confirmation tracking
            var activeCommand = new ActiveCommand
            {
                CommandId = commandId,
                ActuatorId = command.ActuatorId,
                ActuatorCode = command.ActuatorCode,
                Esp32Id = command.Esp32Id,
                Pin = command.Pin,
                StartTime = DateTime.UtcNow,
                Power = command.Power,
                DutyCycle = command.DutyCycle,
                Duration = command.Duration
            };

            _activeCommands[commandId] = activeCommand;

            // Create MQTT payload (no pin locking for reset commands)
            var jobRoutine = CreateJobRoutineDto(command, commandId);
            jobRoutines.Add(jobRoutine);

            // Update actuator state to OFF immediately
            _stateMachine.UpdateState(command.ActuatorId, PowerState.OFF, 0, null, commandId);

            _logger.LogDebug("⚡ Reset command {CommandId} scheduled for {ActuatorCode} - state set to OFF, awaiting firmware confirmation",
                commandId, command.ActuatorCode);
        }

        // Publish all reset commands to MQTT and job schedule
        if (jobRoutines.Any())
        {
            await PublishJobScheduleAsync(esp32Id, jobRoutines);
            _logger.LogInformation("📤 Published {Count} reset commands to MQTT and job schedule - awaiting confirmations", jobRoutines.Count);
        }
    }

    public async Task ResetAllActuatorsAsync(string? esp32Id = null)
    {
        _logger.LogInformation("🔄 Resetting all actuators{Esp32Filter}",
            esp32Id != null ? $" for ESP32 {esp32Id}" : "");

        using var scope = _scopeFactory.CreateScope();
        var internalRoutineRepository = scope.ServiceProvider.GetRequiredService<IInternalRoutineRepository>();
        var actuatorCodeResolver = scope.ServiceProvider.GetRequiredService<IActuatorCodeResolver>();
        var actuatorRepository = scope.ServiceProvider.GetRequiredService<IActuatorRepository>();

        var systemResetRoutine = await internalRoutineRepository.GetByNameAsync("Reinicio del sistema");

        if (systemResetRoutine == null)
        {
            _logger.LogError("❌ Reinicio del sistema routine not found in database");
            throw new InvalidOperationException("Reinicio del sistema routine not found");
        }

        _logger.LogInformation("📋 Found Reinicio del sistema routine with {StepCount} steps", systemResetRoutine.Steps.Count);

        var resolvedCommands = new List<ResolvedCommandDto>();

        foreach (var step in systemResetRoutine.Steps)
        {
            try
            {
                var actuator = await actuatorCodeResolver.ResolveAsync(step.OutputVariable);

                if (actuator == null)
                {
                    _logger.LogWarning("⚠️ Actuator {ActuatorCode} not found, skipping", step.OutputVariable);
                    continue;
                }

                if (actuator.Status != ActuatorStatus.Active)
                {
                    _logger.LogDebug("⏭️ Skipping inactive actuator {ActuatorCode}", actuator.Code);
                    continue;
                }

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
                _logger.LogError(ex, "❌ Error resolving actuator {ActuatorCode}", step.OutputVariable);
            }
        }

        if (resolvedCommands.Count == 0)
        {
            _logger.LogWarning("⚠️ No actuators to reset");
            return;
        }

        var commandsByEsp32 = resolvedCommands.GroupBy(c => c.Esp32Id);

        foreach (var esp32Group in commandsByEsp32)
        {
            var esp32Commands = esp32Group.ToList();
            _logger.LogInformation("🔌 Resetting {Count} actuators for ESP32 {Esp32Id}",
                esp32Commands.Count, esp32Group.Key);

            await SendImmediateResetCommandsAsync(esp32Commands, esp32Group.Key);
        }

        _logger.LogInformation("🏁 Reset complete: {Count} actuators set to OFF", resolvedCommands.Count);
    }

    private class ActiveCommand
    {
        public string CommandId { get; set; } = default!;
        public string ActuatorCode { get; set; } = default!;
        public string ActuatorId { get; set; } = default!;
        public string Esp32Id { get; set; } = default!;
        public string Pin { get; set; } = default!;
        public DateTime StartTime { get; set; }
        public string? Power { get; set; }
        public double? DutyCycle { get; set; }
        public double Duration { get; set; }
    }

    private class PendingCommand
    {
        public string CommandId { get; set; } = default!;
        public string ActuatorCode { get; set; } = default!;
        public string ActuatorId { get; set; } = default!;
        public string Esp32Id { get; set; } = default!;
        public string Pin { get; set; } = default!;
        public DateTime ScheduledTime { get; set; }
        public string? Power { get; set; }
        public double? DutyCycle { get; set; }
        public double Duration { get; set; }
        public JobRoutineDto JobRoutineDto { get; set; } = default!;
    }
}
