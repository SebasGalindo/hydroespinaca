using ActuatorService.Application.DTOs;
using ActuatorService.Domain.Entities;
using ActuatorService.Domain.Interfaces;
using ActuatorService.Application.Interfaces;

namespace ActuatorService.Application.Services;

public class CommandService : ICommandService
{
    private readonly ICommandLogRepository _commandRepo;
    private readonly ICommandPublisher _publisher;

    public CommandService(ICommandLogRepository commandRepo, ICommandPublisher publisher)
    {
        _commandRepo = commandRepo;
        _publisher = publisher;
    }

    public async Task RegisterCommandAsync(CreateCommandDto dto, string? userId)
    {
        var command = new ActuatorCommand
        {
            Id = Guid.NewGuid().ToString("N"),
            ActuatorId = dto.ActuatorId,
            Esp32Id = dto.Esp32Id,
            Action = dto.Action,
            DurationMs = dto.DurationMs,
            Trigger = dto.Trigger,
            RoutineId = dto.RoutineId,
            RoutineStepOrder = dto.RoutineStepOrder,
            UserId = userId,
            Metadata = dto.Metadata is null
            ? null
            : new CommandMetadata
            {
                Source = dto.Metadata.Source,
                FuzzyRule = dto.Metadata.FuzzyRule,
                Inputs = dto.Metadata.Inputs
            },
            Timestamp = DateTime.UtcNow
        };

        await _commandRepo.AddAsync(command);
        await _publisher.PublishAsync(command);
    }
}
