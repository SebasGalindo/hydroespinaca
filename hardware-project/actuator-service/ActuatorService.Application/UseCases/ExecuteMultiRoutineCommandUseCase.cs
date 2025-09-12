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

        // 3. Create job schedule with channel assignments (only updates in-memory state)
        var jobSchedule = await _jobScheduleService.CreateJobScheduleAsync(multiRoutineCommand.Routines);
        
        // 4. Store only the NEW incoming routines in database (avoid duplicates)
        var commandIds = new List<string>();
        var newJobRoutines = new List<JobRoutineDto>();

        // Extract new routines from the job schedule that were just added
        foreach (var channel in jobSchedule.JobSchedule)
        {
            foreach (var routine in channel.Queue)
            {
                // Check if this routine was just generated (part of incoming request)
                var routineId = ExtractRoutineIdFromCommandId(routine.CommandId);
                if (multiRoutineCommand.Routines.Any(r => r.RoutineId == routineId))
                {
                    // This is a new routine - save to database
                    var existingCommand = await _routineCommandRepository.GetByCommandIdAsync(routine.CommandId);
                    if (existingCommand == null) // Only save if not already exists
                    {
                        var routineCommandEntity = new RoutineCommand
                        {
                            CommandId = routine.CommandId,
                            RoutineId = routineId,
                            Esp32Id = jobSchedule.Esp32Id,
                            StatusGeneral = RoutineCommandStatus.SCHEDULED, // All new routines start SCHEDULED
                            CreatedAt = DateTime.UtcNow,
                            Channel = channel.Channel
                        };
                        
                        await _routineCommandRepository.AddAsync(routineCommandEntity);
                        commandIds.Add(routine.CommandId);
                        newJobRoutines.Add(routine);
                    }
                }
            }
        }

        // 5. Update database statuses to match job schedule (first=IN_PROGRESS, rest=SCHEDULED)
        foreach (var channel in jobSchedule.JobSchedule)
        {
            for (int i = 0; i < channel.Queue.Count; i++)
            {
                var routine = channel.Queue[i];
                var command = await _routineCommandRepository.GetByCommandIdAsync(routine.CommandId);
                
                if (command != null)
                {
                    var correctStatus = i == 0 ? RoutineCommandStatus.IN_PROGRESS : RoutineCommandStatus.SCHEDULED;
                    if (command.StatusGeneral != correctStatus)
                    {
                        command.StatusGeneral = correctStatus;
                        await _routineCommandRepository.UpdateAsync(command);
                    }
                }
            }
        }

        // 6. Publish only NEW routines to MQTT (avoid sending existing job schedule)
        if (newJobRoutines.Any())
        {
            var newJobSchedule = new JobScheduleDto
            {
                Esp32Id = jobSchedule.Esp32Id,
                JobSchedule = jobSchedule.JobSchedule
                    .Select(channel => new JobChannelDto
                    {
                        Channel = channel.Channel,
                        Queue = channel.Queue.Where(r => newJobRoutines.Any(nr => nr.CommandId == r.CommandId)).ToList()
                    })
                    .Where(c => c.Queue.Any())
                    .ToList()
            };
            
            await _routineCommandPublisher.PublishJobScheduleAsync(newJobSchedule);
            _logger.LogInformation("📤 Published {NewRoutineCount} new routines to MQTT", newJobRoutines.Count);
        }
        else
        {
            _logger.LogInformation("📋 No new routines to publish - all already exist");
        }

        _logger.LogInformation("Successfully executed multi-routine command with {CommandCount} commands: {CommandIds}", 
            commandIds.Count, string.Join(", ", commandIds));

        return commandIds;
    }

    private string GenerateCommandId(string routineId)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddTHHmmss");
        return $"{routineId}_{timestamp}";
    }

    private string ExtractRoutineIdFromCommandId(string commandId)
    {
        // CommandId format: {routineId}_{timestamp}
        var parts = commandId.Split('_');
        return parts.Length > 0 ? parts[0] : commandId;
    }
}