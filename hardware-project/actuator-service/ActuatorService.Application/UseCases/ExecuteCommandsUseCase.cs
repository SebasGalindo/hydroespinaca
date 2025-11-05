using ActuatorService.Application.DTOs;
using ActuatorService.Application.Helpers;
using ActuatorService.Application.Interfaces;
using ActuatorService.Application.Services;
using ActuatorService.Domain.Entities;
using ActuatorService.Domain.Exceptions;
using ActuatorService.Domain.Interfaces;
using FluentValidation;
using HydroEspinaca.Shared.Constants;
using HydroEspinaca.Shared.DTOs.Actuator;
using HydroEspinaca.Shared.Enums;
using Microsoft.Extensions.Logging;

namespace ActuatorService.Application.UseCases;

public interface IExecuteCommandsUseCase
{
    Task<List<string>> ExecuteAsync(ExecuteCommandsDto executeCommands);
}

public class ExecuteCommandsUseCase : IExecuteCommandsUseCase
{
    private readonly IValidator<ExecuteCommandsDto> _validator;
    private readonly IActuatorCodeResolver _actuatorCodeResolver;
    private readonly ICommandExecutionService _commandExecutionService;
    private readonly IRoutineCommandRepository _routineCommandRepository;
    private readonly IActuatorStateMachine _stateMachine;
    private readonly IWaterRelatedActuatorsLockService _waterLockService;
    private readonly ILogger<ExecuteCommandsUseCase> _logger;

    public ExecuteCommandsUseCase(
        IValidator<ExecuteCommandsDto> validator,
        IActuatorCodeResolver actuatorCodeResolver,
        ICommandExecutionService commandExecutionService,
        IRoutineCommandRepository routineCommandRepository,
        IActuatorStateMachine stateMachine,
        IWaterRelatedActuatorsLockService waterLockService,
        ILogger<ExecuteCommandsUseCase> logger)
    {
        _validator = validator;
        _actuatorCodeResolver = actuatorCodeResolver;
        _commandExecutionService = commandExecutionService;
        _routineCommandRepository = routineCommandRepository;
        _stateMachine = stateMachine;
        _waterLockService = waterLockService;
        _logger = logger;
    }

    public async Task<List<string>> ExecuteAsync(ExecuteCommandsDto executeCommands)
    {
        _logger.LogInformation("Executing {CommandCount} commands", executeCommands.Commands.Count);

        // 1. Validate commands structure
        var validationResult = await _validator.ValidateAsync(executeCommands);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        // 2. Resolve all commands (actuatorCode → physical actuator) and filter redundant commands
        var resolvedCommands = new List<ResolvedCommandDto>();
        var skippedCommands = new List<string>();

        foreach (var command in executeCommands.Commands)
        {
            var actuator = await _actuatorCodeResolver.ResolveAsync(command.ActuatorCode);

            // Validate command parameters for actuator mode
            ValidateCommandForMode(command, actuator);

            // Get current actuator state
            var currentState = _stateMachine.GetState(actuator.Id);
            var power = command.Power ?? command.DutyCycle?.ToString();
            var isPowerOff = power?.Equals(ActuatorConstants.PowerStates.Off, StringComparison.OrdinalIgnoreCase) == true;

            // Skip redundant OFF commands
            if (isPowerOff && currentState?.State == PowerState.OFF)
            {
                _logger.LogInformation("⏭️ Skipping redundant OFF command for {ActuatorCode} - actuator already OFF",
                    command.ActuatorCode);
                skippedCommands.Add(command.ActuatorCode);
                continue;
            }

            // Allow redundant ON commands - they extend/restart actuator duration without needing to turn off/on

            var resolvedCommand = new ResolvedCommandDto
            {
                ActuatorCode = command.ActuatorCode,
                ActuatorId = actuator.Id,
                Esp32Id = actuator.Esp32Id,
                Pin = actuator.Pin,
                Mode = actuator.Mode,
                Power = command.Power,
                DutyCycle = command.DutyCycle,
                Duration = command.Duration
            };

            resolvedCommands.Add(resolvedCommand);
        }

        // If all commands were skipped, return empty list
        if (resolvedCommands.Count == 0)
        {
            _logger.LogInformation("✅ All commands were redundant and skipped: {SkippedCommands}",
                string.Join(", ", skippedCommands));
            return new List<string>();
        }

        // 3. Group by ESP32
        var commandsByEsp32 = resolvedCommands.GroupBy(c => c.Esp32Id).ToList();

        if (commandsByEsp32.Count > 1)
        {
            throw new ArgumentException(
                $"Commands must belong to the same ESP32 device. " +
                $"Found {commandsByEsp32.Count} different ESP32 devices: {string.Join(", ", commandsByEsp32.Select(g => g.Key))}");
        }

        var esp32Id = commandsByEsp32.First().Key;

        // 4. Generate CommandIds and persist to DB BEFORE scheduling
        // This ensures the same CommandId is used in both DB and MQTT messages
        var commandIds = new List<string>();

        foreach (var resolvedCommand in resolvedCommands)
        {
            var power = resolvedCommand.Power ?? resolvedCommand.DutyCycle?.ToString();
            var isPowerOff = power?.Equals(ActuatorConstants.PowerStates.Off, StringComparison.OrdinalIgnoreCase) == true;

            // Check for existing RUNNING command for this actuator
            var existingRunningCommand = await _routineCommandRepository.GetRunningByActuatorCodeAsync(resolvedCommand.ActuatorCode);

            string commandId;

            if (existingRunningCommand != null)
            {
                // Update existing RUNNING command (extend it)
                // Reuse the existing CommandId to maintain consistency
                existingRunningCommand.ExtendedAt = DateTime.UtcNow;

                await _routineCommandRepository.UpdateAsync(existingRunningCommand);
                commandId = existingRunningCommand.CommandId;

                // Assign to DTO so CommandExecutionService uses the same ID
                resolvedCommand.CommandId = commandId;

                _logger.LogInformation("🔄 Extended existing RUNNING command {CommandId} for {ActuatorCode}",
                    commandId, resolvedCommand.ActuatorCode);
            }
            else if (!isPowerOff)
            {
                // Generate CommandId ONCE using the helper
                commandId = CommandIdHelper.GenerateCommandId(resolvedCommand.ActuatorCode);

                // Assign to DTO so CommandExecutionService uses the same ID
                resolvedCommand.CommandId = commandId;

                var routineCommandEntity = new RoutineCommand
                {
                    CommandId = commandId,
                    ActuatorCode = resolvedCommand.ActuatorCode,
                    Esp32Id = esp32Id,
                    StatusGeneral = RoutineCommandStatus.RUNNING,
                    CreatedAt = DateTime.UtcNow
                };

                await _routineCommandRepository.AddAsync(routineCommandEntity);

                _logger.LogInformation("💾 Created new RUNNING command {CommandId} for {ActuatorCode}",
                    commandId, resolvedCommand.ActuatorCode);
            }
            else
            {
                // Power is OFF: don't save to DB, just generate ID for MQTT
                commandId = CommandIdHelper.GenerateCommandId($"{resolvedCommand.ActuatorCode}_off");

                // Assign to DTO so CommandExecutionService uses the same ID
                resolvedCommand.CommandId = commandId;

                _logger.LogInformation("⏹️ Power OFF command for {ActuatorCode} - will publish to MQTT but not save to DB",
                    resolvedCommand.ActuatorCode);
            }

            commandIds.Add(commandId);
        }

        // 5. Schedule commands with execution service (handles MQTT publishing)
        // Now each resolvedCommand has its CommandId already set
        await _commandExecutionService.ScheduleCommandsAsync(resolvedCommands, esp32Id);

        // 6. Evaluate commands for water-related actuator lock
        var commandStates = executeCommands.Commands.Select(cmd =>
        {
            var power = cmd.Power ?? (cmd.DutyCycle.HasValue ? cmd.DutyCycle.Value.ToString() : "UNKNOWN");
            return (cmd.ActuatorCode, power);
        });
        _waterLockService.EvaluateCommands(commandStates);

        if (skippedCommands.Count > 0)
        {
            _logger.LogInformation("✅ Processed {Count} commands for ESP32 {Esp32Id} ({SkippedCount} redundant commands skipped: {SkippedCommands})",
                commandIds.Count, esp32Id, skippedCommands.Count, string.Join(", ", skippedCommands));
        }
        else
        {
            _logger.LogInformation("✅ Processed {Count} commands for ESP32 {Esp32Id}", commandIds.Count, esp32Id);
        }

        return commandIds;
    }

    private void ValidateCommandForMode(ActuatorControlDto command, Domain.Entities.Actuator actuator)
    {
        switch (actuator.Mode)
        {
            case ActuatorMode.DIGITAL:
                if (command.DutyCycle.HasValue)
                {
                    throw new RoutineScheduleConflictException(
                        $"Actuator '{actuator.Code}' is DIGITAL and cannot use 'dutyCycle'. Use 'power' instead.");
                }
                if (string.IsNullOrEmpty(command.Power))
                {
                    throw new RoutineScheduleConflictException(
                        $"Actuator '{actuator.Code}' is DIGITAL and requires 'power' parameter ({ActuatorConstants.PowerStates.On}/{ActuatorConstants.PowerStates.Off}).");
                }
                break;

            case ActuatorMode.PWM:
                if (!string.IsNullOrEmpty(command.Power))
                {
                    throw new RoutineScheduleConflictException(
                        $"Actuator '{actuator.Code}' is PWM and cannot use 'power'. Use 'dutyCycle' instead.");
                }
                if (!command.DutyCycle.HasValue)
                {
                    throw new RoutineScheduleConflictException(
                        $"Actuator '{actuator.Code}' is PWM and requires 'dutyCycle' parameter ({ActuatorConstants.Validation.MinDutyCycle}-{ActuatorConstants.Validation.MaxDutyCycle}).");
                }
                break;
        }
    }
}
