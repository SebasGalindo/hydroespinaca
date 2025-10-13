using ActuatorService.Application.DTOs;
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
    private readonly ILogger<ExecuteCommandsUseCase> _logger;

    public ExecuteCommandsUseCase(
        IValidator<ExecuteCommandsDto> validator,
        IActuatorCodeResolver actuatorCodeResolver,
        ICommandExecutionService commandExecutionService,
        IRoutineCommandRepository routineCommandRepository,
        ILogger<ExecuteCommandsUseCase> logger)
    {
        _validator = validator;
        _actuatorCodeResolver = actuatorCodeResolver;
        _commandExecutionService = commandExecutionService;
        _routineCommandRepository = routineCommandRepository;
        _logger = logger;
    }

    public async Task<List<string>> ExecuteAsync(ExecuteCommandsDto executeCommands)
    {
        _logger.LogInformation("Executing {CommandCount} commands", executeCommands.Commands.Count);

        // 1. Validate commands structure
        var validationResult = await _validator.ValidateAsync(executeCommands);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        // 2. Resolve all commands (actuatorCode → physical actuator)
        var resolvedCommands = new List<ResolvedCommandDto>();

        foreach (var command in executeCommands.Commands)
        {
            var actuator = await _actuatorCodeResolver.ResolveAsync(command.ActuatorCode);

            // Validate command parameters for actuator mode
            ValidateCommandForMode(command, actuator);

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

        // 3. Group by ESP32
        var commandsByEsp32 = resolvedCommands.GroupBy(c => c.Esp32Id).ToList();

        if (commandsByEsp32.Count > 1)
        {
            throw new ArgumentException(
                $"Commands must belong to the same ESP32 device. " +
                $"Found {commandsByEsp32.Count} different ESP32 devices: {string.Join(", ", commandsByEsp32.Select(g => g.Key))}");
        }

        var esp32Id = commandsByEsp32.First().Key;

        // 4. Schedule commands with execution service (handles pin locking and MQTT publishing)
        var commandIds = await _commandExecutionService.ScheduleCommandsAsync(resolvedCommands, esp32Id);

        // 5. Store commands in database for tracking completion
        foreach (var (resolvedCommand, commandId) in resolvedCommands.Zip(commandIds, (cmd, id) => (cmd, id)))
        {
            var existingCommand = await _routineCommandRepository.GetByCommandIdAsync(commandId);
            if (existingCommand == null)
            {
                var stepMapping = new RoutineStepMapping
                {
                    Pin = resolvedCommand.Pin,
                    ActuatorId = resolvedCommand.ActuatorId,
                    Duration = resolvedCommand.Duration,
                    DutyCycle = resolvedCommand.DutyCycle,
                    Power = resolvedCommand.Power
                };

                var routineCommandEntity = new RoutineCommand
                {
                    CommandId = commandId,
                    RoutineId = $"command_{resolvedCommand.ActuatorCode}",
                    Esp32Id = esp32Id,
                    StatusGeneral = RoutineCommandStatus.SCHEDULED,
                    CreatedAt = DateTime.UtcNow,
                    StepMappings = new List<RoutineStepMapping> { stepMapping }
                };

                await _routineCommandRepository.AddAsync(routineCommandEntity);
                _logger.LogDebug("💾 Saved command {CommandId} to database", commandId);
            }
        }

        _logger.LogInformation("✅ Executed {Count} commands for ESP32 {Esp32Id}", commandIds.Count, esp32Id);

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
