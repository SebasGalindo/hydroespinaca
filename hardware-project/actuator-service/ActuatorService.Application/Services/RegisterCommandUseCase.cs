using ActuatorService.Application.Interfaces;
using ActuatorService.Application.Mappers;
using ActuatorService.Domain.Interfaces;
using ActuatorService.Domain.Exceptions;
using FluentValidation;
using HydroEspinaca.Shared.DTOs.Actuator;
using HydroEspinaca.Shared.Enums;
using HydroEspinaca.Shared.Errors;
using MongoDB.Bson;

namespace ActuatorService.Application.Services;

public class RegisterCommandService : ICommandService
{
    private readonly ICommandLogRepository _commandRepo;
    private readonly ICommandPublisher _publisher;
    private readonly IValidator<CreateCommandDto> _createValidator;
    private readonly IEsp32ValidationService _esp32Validator;
    private readonly IActuatorRepository _repo;
    public RegisterCommandService(
        ICommandLogRepository commandRepo,
        ICommandPublisher publisher,
        IValidator<CreateCommandDto> createValidator,
        IEsp32ValidationService esp32Validator,
        IActuatorRepository repo)
    {
        _commandRepo = commandRepo;
        _publisher = publisher;
        _createValidator = createValidator;
        _esp32Validator = esp32Validator;
        _repo = repo;
    }

    public async Task AddAsync(CreateCommandDto dto, string? userId)
    {
        if (!ObjectId.TryParse(dto.Esp32Id, out _))
            throw new ArgumentException("Formato de ID no válido. Se esperaba una cadena hexadecimal de 24 caracteres.");

        if (!ObjectId.TryParse(dto.ActuatorId, out _))
            throw new ArgumentException("Formato de ID no válido. Se esperaba una cadena hexadecimal de 24 caracteres.");

        if (!Enum.TryParse<TriggerType>(dto.Trigger, true, out var parsedTrigger))
            throw new ArgumentException($"Trigger '{dto.Trigger}' no es válido. Valores permitidos: {string.Join(", ", Enum.GetNames(typeof(TriggerType)))}");

        if (!await _esp32Validator.ExistsAsync(dto.Esp32Id))
            throw new Esp32NotFoundException("unknown");

        var actuator = await _repo.GetByIdAsync(dto.ActuatorId);
        if (actuator is null)
            throw new ActuatorNotFoundException(dto.ActuatorId);

        // Structural validation with FluentValidation
        var validationResult = await _createValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        // Contextual business rules
        if (parsedTrigger == TriggerType.Routine)
        {
            if (string.IsNullOrWhiteSpace(dto.RoutineId))
                throw new ArgumentException("RoutineId es obligatorio cuando Trigger es 'Routine'.");

            if (dto.RoutineStepOrder is null || dto.RoutineStepOrder < 0)
                throw new ArgumentException("RoutineStepOrder debe ser mayor o igual a 0 cuando Trigger es 'Routine'.");
        }

        if (dto.Metadata is not null)
        {
            if (string.IsNullOrWhiteSpace(dto.Metadata.Source))
                throw new ArgumentException("Metadata.Source no puede estar vacío si se proporciona Metadata.");
        }

        var command = CommandMapper.ToEntity(dto, userId);

        await _commandRepo.AddAsync(command);
        await _publisher.PublishAsync(command);
    }


    public async Task<List<ActuatorCommandDto>> GetByActuatorIdAsync(string actuatorId)
    {
        if (!ObjectId.TryParse(actuatorId, out _))
            throw new ArgumentException("Formato de ID no válido. Se esperaba una cadena hexadecimal de 24 caracteres.");

        var entity = await _repo.GetByIdAsync(actuatorId);
        if (entity is null)
            throw new ActuatorNotFoundException(actuatorId);

        var commands = await _commandRepo.GetByActuatorIdAsync(actuatorId);
        return commands.Select(CommandMapper.ToDto).ToList();
    }

    public async Task<List<ActuatorCommandDto>> GetByDateRangeAsync(DateTime from, DateTime to)
    {
        var commands = await _commandRepo.GetByDateRangeAsync(from, to);
        return commands.Select(CommandMapper.ToDto).ToList();
    }
}
