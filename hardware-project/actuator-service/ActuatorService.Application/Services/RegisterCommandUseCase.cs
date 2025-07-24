using ActuatorService.Application.Interfaces;
using ActuatorService.Application.Mappers;
using ActuatorService.Domain.Interfaces;
using FluentValidation;
using HydroEspinaca.Shared.DTOs.Actuator;
using HydroEspinaca.Shared.Errors;

namespace ActuatorService.Application.Services;

public class RegisterCommandService : ICommandService
{
    private readonly ICommandLogRepository _commandRepo;
    private readonly ICommandPublisher _publisher;
    private readonly IValidator<CreateCommandDto> _createValidator;

    public RegisterCommandService(
        ICommandLogRepository commandRepo,
        ICommandPublisher publisher,
        IValidator<CreateCommandDto> createValidator)
    {
        _commandRepo = commandRepo;
        _publisher = publisher;
        _createValidator = createValidator;
    }

    public async Task AddAsync(CreateCommandDto dto, string? userId)
    {
        var validationResult = await _createValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
            throw new FluentValidation.ValidationException(validationResult.Errors);

        var command = CommandMapper.ToEntity(dto, userId);

        await _commandRepo.AddAsync(command);
        await _publisher.PublishAsync(command);
    }

    public async Task<List<ActuatorCommandDto>> GetByActuatorIdAsync(string actuatorId)
    {
        var commands = await _commandRepo.GetByActuatorIdAsync(actuatorId);

        if (commands == null || !commands.Any())
            throw new NotFoundException($"No commands found for actuator {actuatorId}");

        return commands.Select(CommandMapper.ToDto).ToList();
    }

    public async Task<List<ActuatorCommandDto>> GetByDateRangeAsync(DateTime from, DateTime to)
    {
        var commands = await _commandRepo.GetByDateRangeAsync(from, to);
        return commands.Select(CommandMapper.ToDto).ToList();
    }
}
