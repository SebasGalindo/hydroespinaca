using ActuatorService.Application.Interfaces;
using ActuatorService.Application.Mappers;
using ActuatorService.Domain.Exceptions;
using ActuatorService.Domain.Interfaces;
using FluentValidation;
using HydroEspinaca.Shared.Constants;
using HydroEspinaca.Shared.DTOs.Actuator;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;

namespace ActuatorService.Application.Services;

public class ControlOutputServiceApplication : IControlOutputService
{
    private readonly IControlOutputRepository _repo;
    private readonly IActuatorRepository _actuatorRepo;
    private readonly IValidator<CreateControlOutputDto> _createValidator;
    private readonly IValidator<UpdateControlOutputDto> _updateValidator;
    private readonly ILogger<ControlOutputServiceApplication> _logger;

    public ControlOutputServiceApplication(
        IControlOutputRepository repo,
        IActuatorRepository actuatorRepo,
        IValidator<CreateControlOutputDto> createValidator,
        IValidator<UpdateControlOutputDto> updateValidator,
        ILogger<ControlOutputServiceApplication> logger)
    {
        _repo = repo;
        _actuatorRepo = actuatorRepo;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _logger = logger;
    }

    public async Task<List<ControlOutputDto>> GetAllAsync()
    {
        var entities = await _repo.GetAllAsync();
        return entities.Select(ControlOutputMapper.ToDto).ToList();
    }

    public async Task<ControlOutputDto> GetByIdAsync(string id)
    {
        if (!ObjectId.TryParse(id, out _))
            throw new ArgumentException($"Formato de ID no válido. Se esperaba una cadena hexadecimal de {ActuatorConstants.Validation.ObjectIdLength} caracteres.");

        var entity = await _repo.GetByIdAsync(id);
        if (entity is null)
            throw new ControlOutputNotFoundException(id);

        return ControlOutputMapper.ToDto(entity);
    }

    public async Task<List<ControlOutputDto>> GetByActuatorIdAsync(string actuatorId)
    {
        if (!ObjectId.TryParse(actuatorId, out _))
            throw new ArgumentException($"Formato de ID no válido. Se esperaba una cadena hexadecimal de {ActuatorConstants.Validation.ObjectIdLength} caracteres.");

        var entities = await _repo.GetByActuatorIdAsync(actuatorId);
        return entities.Select(ControlOutputMapper.ToDto).ToList();
    }

    public async Task<string> AddAsync(CreateControlOutputDto dto)
    {
        // Validate DTO
        var validationResult = await _createValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
            throw new ValidationException("Validation failed.", validationResult.Errors);

        // Verify actuator exists
        if (!ObjectId.TryParse(dto.ActuatorId, out _))
            throw new ArgumentException($"Formato de ID de actuador no válido. Se esperaba una cadena hexadecimal de {ActuatorConstants.Validation.ObjectIdLength} caracteres.");

        var actuator = await _actuatorRepo.GetByIdAsync(dto.ActuatorId);
        if (actuator is null)
            throw new ActuatorNotFoundException(dto.ActuatorId);

        var entity = ControlOutputMapper.ToEntity(dto);
        await _repo.AddAsync(entity);

        _logger.LogInformation("Control output '{Name}' created with ID '{Id}' for actuator '{ActuatorId}'",
            entity.Name, entity.Id, entity.ActuatorId);

        return entity.Id;
    }

    public async Task UpdateAsync(string id, UpdateControlOutputDto dto)
    {
        if (!ObjectId.TryParse(id, out _))
            throw new ArgumentException($"Formato de ID no válido. Se esperaba una cadena hexadecimal de {ActuatorConstants.Validation.ObjectIdLength} caracteres.");

        // Validate DTO
        var validationResult = await _updateValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
            throw new ValidationException("Validation failed.", validationResult.Errors);

        // Verify control output exists
        var entity = await _repo.GetByIdAsync(id);
        if (entity is null)
            throw new ControlOutputNotFoundException(id);

        // Verify actuator exists
        if (!ObjectId.TryParse(dto.ActuatorId, out _))
            throw new ArgumentException($"Formato de ID de actuador no válido. Se esperaba una cadena hexadecimal de {ActuatorConstants.Validation.ObjectIdLength} caracteres.");

        var actuator = await _actuatorRepo.GetByIdAsync(dto.ActuatorId);
        if (actuator is null)
            throw new ActuatorNotFoundException(dto.ActuatorId);

        ControlOutputMapper.MapUpdate(dto, entity);
        await _repo.UpdateAsync(entity);

        _logger.LogInformation("Control output '{Id}' updated", id);
    }

    public async Task DeleteAsync(string id)
    {
        if (!ObjectId.TryParse(id, out _))
            throw new ArgumentException($"Formato de ID no válido. Se esperaba una cadena hexadecimal de {ActuatorConstants.Validation.ObjectIdLength} caracteres.");

        var entity = await _repo.GetByIdAsync(id);
        if (entity is null)
            throw new ControlOutputNotFoundException(id);

        await _repo.DeleteAsync(id);

        _logger.LogInformation("Control output '{Id}' deleted", id);
    }
}
