using ActuatorService.Application.DTOs;
using ActuatorService.Application.Interfaces;
using ActuatorService.Domain.Entities;
using ActuatorService.Domain.Interfaces;
using FluentValidation;
using HydroEspinaca.Shared.DTOs.Actuator;
using HydroEspinaca.Shared.Enums;
using Microsoft.Extensions.Logging;

namespace ActuatorService.Application.UseCases;

public class ExecuteMultiRoutineCommandUseCase : IExecuteMultiRoutineCommandUseCase
{
    private readonly IValidator<MultiRoutineCommandDto> _validator;
    private readonly IRoutineValidationService _routineValidationService;
    private readonly IRoutineExecutionService _routineExecutionService;
    private readonly IRoutineCommandRepository _routineCommandRepository;
    private readonly ICommandFilterService _commandFilterService;
    private readonly ILogger<ExecuteMultiRoutineCommandUseCase> _logger;

    public ExecuteMultiRoutineCommandUseCase(
        IValidator<MultiRoutineCommandDto> validator,
        IRoutineValidationService routineValidationService,
        IRoutineExecutionService routineExecutionService,
        IRoutineCommandRepository routineCommandRepository,
        ICommandFilterService commandFilterService,
        ILogger<ExecuteMultiRoutineCommandUseCase> logger)
    {
        _validator = validator;
        _routineValidationService = routineValidationService;
        _routineExecutionService = routineExecutionService;
        _routineCommandRepository = routineCommandRepository;
        _commandFilterService = commandFilterService;
        _logger = logger;
    }

    public async Task<List<string>> ExecuteAsync(MultiRoutineCommandDto multiRoutineCommand)
    {
        _logger.LogInformation("Executing multi-routine command with {RoutineCount} routines",
            multiRoutineCommand.Routines.Count);

        // 1. Validate multi-routine DTO structure
        var validationResult = await _validator.ValidateAsync(multiRoutineCommand);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        // 2. Apply intelligent command filter (remove redundant commands)
        var filteredRoutines = await _commandFilterService.FilterCommandsAsync(multiRoutineCommand.Routines);

        if (filteredRoutines.Count == 0)
        {
            _logger.LogInformation("🚫 All routines were filtered out as redundant, no commands to execute");
            return new List<string>();
        }

        _logger.LogInformation("✅ Filter passed {AcceptedCount}/{TotalCount} routines",
            filteredRoutines.Count, multiRoutineCommand.Routines.Count);

        // 3. Validate and resolve all routine steps (outputVariable → physical actuator data)
        var resolvedRoutines = new List<ResolvedRoutineDto>();
        foreach (var routine in filteredRoutines)
        {
            var resolvedSteps = await _routineValidationService.ValidateAndResolveStepsAsync(routine.Steps);

            // Ensure all steps belong to same ESP32
            var esp32Ids = resolvedSteps.Select(s => s.Esp32Id).Distinct().ToList();
            if (esp32Ids.Count > 1)
            {
                throw new ArgumentException(
                    $"Routine '{routine.RoutineId}' contains steps from multiple ESP32 devices: {string.Join(", ", esp32Ids)}. " +
                    "All steps in a routine must belong to the same ESP32.");
            }

            resolvedRoutines.Add(new ResolvedRoutineDto
            {
                RoutineId = routine.RoutineId,
                Esp32Id = esp32Ids.First(),
                ResolvedSteps = resolvedSteps
            });
        }

        // Ensure all routines belong to same ESP32
        var allEsp32Ids = resolvedRoutines.Select(r => r.Esp32Id).Distinct().ToList();
        if (allEsp32Ids.Count > 1)
        {
            throw new ArgumentException(
                $"Multi-routine command contains routines from multiple ESP32 devices: {string.Join(", ", allEsp32Ids)}. " +
                "All routines must belong to the same ESP32.");
        }

        var esp32Id = allEsp32Ids.First();

        // 4. Schedule routines with execution service (handles pin locking and MQTT publishing)
        var commandIds = await _routineExecutionService.ScheduleRoutinesAsync(resolvedRoutines, esp32Id);

        // 5. Store routines in database
        foreach (var resolvedRoutine in resolvedRoutines)
        {
            var commandId = commandIds.FirstOrDefault(id => id.Contains(resolvedRoutine.RoutineId));
            if (commandId == null) continue;

            var existingCommand = await _routineCommandRepository.GetByCommandIdAsync(commandId);
            if (existingCommand == null)
            {
                var stepMappings = resolvedRoutine.ResolvedSteps.Select(step => new RoutineStepMapping
                {
                    Pin = step.Pin,
                    ActuatorId = step.ActuatorId,
                    OutputVariableId = step.OutputVariableId
                }).ToList();

                var routineCommandEntity = new RoutineCommand
                {
                    CommandId = commandId,
                    RoutineId = resolvedRoutine.RoutineId,
                    Esp32Id = esp32Id,
                    StatusGeneral = RoutineCommandStatus.SCHEDULED, // Will be IN_PROGRESS if activated immediately
                    CreatedAt = DateTime.UtcNow,
                    StepMappings = stepMappings
                };

                await _routineCommandRepository.AddAsync(routineCommandEntity);
                _logger.LogDebug("💾 Saved routine {CommandId} to database", commandId);
            }
        }

        _logger.LogInformation("✅ Executed {Count} routines for ESP32 {Esp32Id}", commandIds.Count, esp32Id);

        return commandIds;
    }
}
