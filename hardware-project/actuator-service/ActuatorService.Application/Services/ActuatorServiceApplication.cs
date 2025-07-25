using ActuatorService.Application.Interfaces;
using ActuatorService.Application.Mappers;
using ActuatorService.Domain.Interfaces;
using FluentValidation;
using HydroEspinaca.Shared.DTOs.Actuator;
using HydroEspinaca.Shared.Errors;

namespace ActuatorService.Application.Services;

public class ActuatorServiceApplication : IActuatorService
{
    private readonly IActuatorRepository _repo;
    private readonly IValidator<CreateActuatorDto> _createValidator;
    private readonly IValidator<UpdateActuatorDto> _updateValidator;

    public ActuatorServiceApplication(
        IActuatorRepository repo,
        IValidator<CreateActuatorDto> createValidator,
        IValidator<UpdateActuatorDto> updateValidator)
    {
        _repo = repo;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<List<ActuatorDto>> GetAllAsync()
    {
        var entities = await _repo.GetAllAsync();

        if (entities is null || !entities.Any())
            throw new NotFoundException("No actuators found.");

        return entities.Select(ActuatorMapper.ToDto).ToList();
    }

    public async Task<ActuatorDto> GetByIdAsync(string id)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity is null)
            throw new NotFoundException($"Actuator {id} not found.");

        return ActuatorMapper.ToDto(entity);
    }

    public async Task<List<ActuatorDto>> GetByEsp32IdAsync(string esp32Id)
    {
        var entities = await _repo.GetByEsp32IdAsync(esp32Id);
        return entities.Select(ActuatorMapper.ToDto).ToList();
    }

    public async Task<string> AddAsync(CreateActuatorDto dto)
    {
        var validationResult = await _createValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
            throw new ValidationException("Validation failed.", validationResult.Errors);

        var entity = ActuatorMapper.ToEntity(dto);
        await _repo.AddAsync(entity);
        return entity.Id;
    }

    public async Task UpdateAsync(string id, UpdateActuatorDto dto)
    {
        var validationResult = await _updateValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
            throw new ValidationException("Validation failed.", validationResult.Errors);

        var entity = await _repo.GetByIdAsync(id);
        if (entity is null)
            throw new NotFoundException($"Actuator {id} not found.");

        ActuatorMapper.MapUpdate(dto, entity);
        await _repo.UpdateAsync(entity);
    }

    public async Task DeleteAsync(string id)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity is null)
            throw new NotFoundException($"Actuator {id} not found.");

        await _repo.DeleteAsync(id);
    }
}
