using FluentValidation;
using HydroEspinaca.Shared.DTOs.Variables;
using HydroEspinaca.Shared.Enums;
using HydroEspinaca.Shared.Errors;
using MongoDB.Bson;
using SensorService.Application.Interfaces;
using SensorService.Application.Mappers;
using SensorService.Domain.Interfaces;
using SensorService.Domain.Exceptions;

namespace SensorService.Application.Services;

public class VariableService : IVariableService
{
    private readonly IVariableRepository _repo;
    private readonly IValidator<VariableCreateDto> _createValidator;
    private readonly IValidator<VariableUpdateDto> _updateValidator;

    public VariableService(
        IVariableRepository repo,
        IValidator<VariableCreateDto> createValidator,
        IValidator<VariableUpdateDto> updateValidator)
    {
        _repo = repo;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<List<VariableDto>> GetAllAsync()
    {
        var list = await _repo.GetAllAsync();
        return list.Select(VariableMapper.ToDto).ToList();
    }

    public async Task<VariableDto?> GetByIdAsync(string id)
    {
        if (!ObjectId.TryParse(id, out _))
            throw new ValidationException("Formato de ID no válido. Se esperaba una cadena hexadecimal de 24 caracteres.");

        var variable = await _repo.GetByIdAsync(id);
        if (variable is null)
            throw new SensorDataNotFoundException($"Variable con ID no encontrada");

        return VariableMapper.ToDto(variable);
    }

    public async Task AddAsync(VariableCreateDto dto)
    {

        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        if (!Enum.TryParse<VariableTypes>(dto.Type, true, out var parsedType))
            throw new ArgumentException($"Tipo '{dto.Type}' no es válido. Valores permitidos: {string.Join(", ", Enum.GetNames(typeof(VariableTypes)))}");

        var entity = VariableMapper.ToEntity(dto);
        await _repo.CreateAsync(entity);
    }

    public async Task UpdateAsync(string id, VariableUpdateDto dto)
    {
        var validation = await _updateValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var entity = await _repo.GetByIdAsync(id);
        if (entity == null)
            throw new SensorDataNotFoundException($"Variable con ID no encontrada");

        if (!Enum.TryParse<VariableTypes>(dto.Type, true, out var parsedType))
            throw new ArgumentException($"Tipo '{dto.Type}' no es válido. Valores permitidos: {string.Join(", ", Enum.GetNames(typeof(VariableTypes)))}");

        VariableMapper.MapUpdate(dto, entity);
        await _repo.UpdateAsync(entity);
    }

    public async Task DeleteAsync(string id)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity == null)
            throw new SensorDataNotFoundException($"Variable con ID no encontrada");

        await _repo.DeleteAsync(id);
    }
}
