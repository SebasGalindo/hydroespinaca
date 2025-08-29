using ActuatorService.Application.Interfaces;
using ActuatorService.Application.Services;
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
    private readonly IJobScheduleService _jobScheduleService;
    private readonly IRoutineCommandRepository _routineCommandRepository;
    private readonly IRoutineCommandPublisher _routineCommandPublisher;
    private readonly ILogger<ExecuteMultiRoutineCommandUseCase> _logger;

    public ExecuteMultiRoutineCommandUseCase(
        IValidator<MultiRoutineCommandDto> validator,
        IRoutineValidationService routineValidationService,
        IJobScheduleService jobScheduleService,
        IRoutineCommandRepository routineCommandRepository,
        IRoutineCommandPublisher routineCommandPublisher,
        ILogger<ExecuteMultiRoutineCommandUseCase> logger)
    {
        _validator = validator;
        _routineValidationService = routineValidationService;
        _jobScheduleService = jobScheduleService;
        _routineCommandRepository = routineCommandRepository;
        _routineCommandPublisher = routineCommandPublisher;
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

        // 2. Validate all routine steps
        foreach (var routine in multiRoutineCommand.Routines)
        {
            await _routineValidationService.ValidateRoutineStepsAsync(routine.Steps);
        }

        // 3. Create job schedule with channel assignments
        var jobSchedule = await _jobScheduleService.CreateJobScheduleAsync(multiRoutineCommand.Routines);
        
        // 4. Store all routine commands in database with initial scheduled status
        var commandIds = new List<string>();
        foreach (var channel in jobSchedule.JobSchedule)
        {
            foreach (var routine in channel.Queue)
            {
                var routineCommandEntity = new RoutineCommand
                {
                    CommandId = routine.CommandId,
                    RoutineId = ExtractRoutineIdFromCommandId(routine.CommandId),
                    StatusGeneral = RoutineCommandStatus.SCHEDULED,
                    CreatedAt = DateTime.UtcNow,
                    Channel = channel.Channel
                };
                
                await _routineCommandRepository.AddAsync(routineCommandEntity);
                commandIds.Add(routine.CommandId);
            }
        }

        // 5. Publish job schedule to MQTT
        await _routineCommandPublisher.PublishJobScheduleAsync(jobSchedule);

        _logger.LogInformation("Successfully executed multi-routine command with {CommandCount} commands: {CommandIds}", 
            commandIds.Count, string.Join(", ", commandIds));

        return commandIds;
    }

    private string ExtractRoutineIdFromCommandId(string commandId)
    {
        // CommandId format: {routineId}_{timestamp}
        var parts = commandId.Split('_');
        return parts.Length > 0 ? parts[0] : commandId;
    }
}