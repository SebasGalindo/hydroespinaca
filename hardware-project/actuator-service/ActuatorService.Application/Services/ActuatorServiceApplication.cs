using ActuatorService.Application.Interfaces;
using ActuatorService.Application.Mappers;
using ActuatorService.Domain.Interfaces;
using FluentValidation;
using HydroEspinaca.Shared.Constants;
using HydroEspinaca.Shared.DTOs.Actuator;
using HydroEspinaca.Shared.Enums;
using HydroEspinaca.Shared.Errors;
using MongoDB.Bson;

namespace ActuatorService.Application.Services;

public class ActuatorServiceApplication : IActuatorService
{
    private readonly IActuatorRepository _repo;
    private readonly IValidator<CreateActuatorDto> _createValidator;
    private readonly IValidator<UpdateActuatorDto> _updateValidator;
    private readonly IEsp32ValidationService _esp32Validator;

    public ActuatorServiceApplication(
        IActuatorRepository repo,
        IValidator<CreateActuatorDto> createValidator,
        IValidator<UpdateActuatorDto> updateValidator,
        IEsp32ValidationService esp32Validator
        )
    {
        _repo = repo;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _esp32Validator = esp32Validator;
    }

    public async Task<List<ActuatorDto>> GetAllAsync()
    {
        var entities = await _repo.GetAllAsync();
        return entities.Select(ActuatorMapper.ToDto).ToList();
    }

    public async Task<ActuatorDto> GetByIdAsync(string id)
    {
        if (!ObjectId.TryParse(id, out _))
            throw new ArgumentException($"Formato de ID no válido. Se esperaba una cadena hexadecimal de {ActuatorConstants.Validation.ObjectIdLength} caracteres.");

        var entity = await _repo.GetByIdAsync(id);
        if (entity is null)
            throw new NotFoundException($"Actuator con el ID {id} no encontrado.");

        return ActuatorMapper.ToDto(entity);
    }

    public async Task<List<ActuatorDto>> GetByEsp32IdAsync(string esp32Id)
    {
        if (!ObjectId.TryParse(esp32Id, out _))
            throw new ArgumentException($"Formato de ID no válido. Se esperaba una cadena hexadecimal de {ActuatorConstants.Validation.ObjectIdLength} caracteres.");

        if (!await _esp32Validator.ExistsAsync(esp32Id))
            throw new NotFoundException("El ID del ESP32 no existe");

        var entities = await _repo.GetByEsp32IdAsync(esp32Id);
        return entities.Select(ActuatorMapper.ToDto).ToList();
    }

    public async Task<string> AddAsync(CreateActuatorDto dto)
    {
        if (!ObjectId.TryParse(dto.Esp32Id, out _))
            throw new ArgumentException($"Formato de ID no válido. Se esperaba una cadena hexadecimal de {ActuatorConstants.Validation.ObjectIdLength} caracteres.");

        if (!await _esp32Validator.ExistsAsync(dto.Esp32Id))
            throw new NotFoundException("El ID del ESP32 no existe");

        var validationResult = await _createValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
            throw new ValidationException("Validation failed.", validationResult.Errors);

        var entity = ActuatorMapper.ToEntity(dto);
        await _repo.AddAsync(entity);
        return entity.Id;
    }

    public async Task UpdateAsync(string id, UpdateActuatorDto dto)
    {

        if (!ObjectId.TryParse(id, out _))
            throw new ArgumentException($"Formato de ID no válido. Se esperaba una cadena hexadecimal de {ActuatorConstants.Validation.ObjectIdLength} caracteres.");

        if (!Enum.TryParse<ActuatorStatus>(dto.Status, true, out var parsedStatus))
            throw new ArgumentException($"Estado '{dto.Status}' no es válido. Valores permitidos: {string.Join(", ", Enum.GetNames(typeof(ActuatorStatus)))}");

        var validationResult = await _updateValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
            throw new ValidationException("Validation failed.", validationResult.Errors);

        var entity = await _repo.GetByIdAsync(id);
        if (entity is null)
            throw new NotFoundException($"Actuador {id} no encontrado.");

        ActuatorMapper.MapUpdate(dto, entity);
        await _repo.UpdateAsync(entity);
    }

    public async Task DeleteAsync(string id)
    {
        if (!ObjectId.TryParse(id, out _))
            throw new ArgumentException($"Formato de ID no válido. Se esperaba una cadena hexadecimal de {ActuatorConstants.Validation.ObjectIdLength} caracteres.");

        var entity = await _repo.GetByIdAsync(id);
        if (entity is null)
            throw new NotFoundException($"Actuador {id} no encontrado.");

        await _repo.DeleteAsync(id);
    }
}
