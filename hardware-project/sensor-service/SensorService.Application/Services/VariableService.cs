using SensorService.Application.DTOs.Variable;
using SensorService.Application.Interfaces;
using SensorService.Domain.Interfaces;

namespace SensorService.Application.Services;

public class VariableService : IVariableService
{
    private readonly IVariableRepository _repo;

    public VariableService(IVariableRepository repo)
    {
        _repo = repo;
    }

    public async Task<List<VariableDto>> GetAllAsync()
    {
        var list = await _repo.GetAllAsync();
        return list.Select(x => new VariableDto
        {
            Id = x.Id,
            Name = x.Name,
            Unit = x.Unit,
            Description = x.Description,
            MinValue = x.MinValue,
            MaxValue = x.MaxValue,
            Type = x.Type,
            LastModified = x.LastModified
        }).ToList();
    }

    public async Task<VariableDto?> GetByIdAsync(string id)
    {
        var v = await _repo.GetByIdAsync(id);
        return v is null ? null : new VariableDto
        {
            Id = v.Id,
            Name = v.Name,
            Unit = v.Unit,
            Description = v.Description,
            MinValue = v.MinValue,
            MaxValue = v.MaxValue,
            Type = v.Type,
            LastModified = v.LastModified
        };
    }

    public async Task AddAsync(VariableCreateDto dto)
    {
        var entity = new Variable
        {
            Id = dto.Id,
            Name = dto.Name,
            Unit = dto.Unit,
            Description = dto.Description,
            MinValue = dto.MinValue,
            MaxValue = dto.MaxValue,
            Type = dto.Type,
            LastModified = DateTime.UtcNow
        };
        await _repo.CreateAsync(entity);
    }

    public async Task UpdateAsync(string id, VariableUpdateDto dto)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity == null) throw new Exception($"Variable '{id}' no encontrada.");

        entity.Name = dto.Name;
        entity.Unit = dto.Unit;
        entity.Description = dto.Description;
        entity.MinValue = dto.MinValue;
        entity.MaxValue = dto.MaxValue;
        entity.Type = dto.Type;
        entity.LastModified = DateTime.UtcNow;

        await _repo.UpdateAsync(entity);
    }

    public async Task DeleteAsync(string id)
    {
        await _repo.DeleteAsync(id);
    }
}
