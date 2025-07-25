using FluentValidation;
using HydroEspinaca.Shared.DTOs.Variables;
using SensorService.Application.Interfaces;
using SensorService.Application.Mappers;
using SensorService.Domain.Interfaces;

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
        var v = await _repo.GetByIdAsync(id);
        return v is null ? null : VariableMapper.ToDto(v);
    }

    public async Task AddAsync(VariableCreateDto dto)
    {
        await _createValidator.ValidateAndThrowAsync(dto);
        var entity = VariableMapper.ToEntity(dto);
        await _repo.CreateAsync(entity);
    }

    public async Task UpdateAsync(string id, VariableUpdateDto dto)
    {
        await _updateValidator.ValidateAndThrowAsync(dto);
        var entity = await _repo.GetByIdAsync(id);
        if (entity == null)
            throw new Exception($"Variable '{id}' no encontrada.");

        VariableMapper.MapUpdate(dto, entity);
        await _repo.UpdateAsync(entity);
    }

    public async Task DeleteAsync(string id)
    {
        await _repo.DeleteAsync(id);
    }
}
