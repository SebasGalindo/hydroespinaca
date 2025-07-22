using ActuatorService.Application.DTOs;
using ActuatorService.Application.Interfaces;
using ActuatorService.Domain.Entities;
using ActuatorService.Domain.Interfaces;
using HydroEspinaca.Shared.Enums;

namespace ActuatorService.Application.Services;
public class ActuatorService : IActuatorService
{
    private readonly IActuatorRepository _repo;

    public ActuatorService(IActuatorRepository repo)
    {
        _repo = repo;
    }

    public async Task<List<ActuatorDto>> GetAllAsync()
    {
        var entities = await _repo.GetAllAsync();
        return entities.Select(ToDto).ToList();
    }

    public async Task<ActuatorDto?> GetByIdAsync(string id)
    {
        var entity = await _repo.GetByIdAsync(id);
        return entity is null ? null : ToDto(entity);
    }

    public async Task<List<ActuatorDto>> GetByEsp32IdAsync(string esp32Id)
    {
        var entities = await _repo.GetByEsp32IdAsync(esp32Id);
        return entities.Select(ToDto).ToList();
    }

    public async Task<string> CreateAsync(CreateActuatorDto dto)
    {
        var entity = new Actuator
        {
            Id = Guid.NewGuid().ToString("N"),
            Esp32Id = dto.Esp32Id,
            Name = dto.Name,
            Type = Enum.Parse<ActuatorType>(dto.Type),
            PhysicalId = dto.PhysicalId,
            Pin = dto.Pin,
            Location = dto.Location,
            Status = ActuatorStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        await _repo.AddAsync(entity);
        return entity.Id;
    }

    public async Task UpdateAsync(UpdateActuatorDto dto)
    {
        var entity = await _repo.GetByIdAsync(dto.Id)
            ?? throw new InvalidOperationException($"Actuator {dto.Id} not found.");

        entity.Name = dto.Name;
        entity.Location = dto.Location;
        entity.Pin = dto.Pin;
        entity.Status = Enum.Parse<ActuatorStatus>(dto.Status);

        await _repo.UpdateAsync(entity);
    }

    public async Task DeleteAsync(string id)
    {
        await _repo.DeleteAsync(id);
    }

    private static ActuatorDto ToDto(Actuator x) => new()
    {
        Id = x.Id,
        Esp32Id = x.Esp32Id,
        Name = x.Name,
        Type = x.Type.ToString(),
        PhysicalId = x.PhysicalId,
        Pin = x.Pin,
        Location = x.Location,
        Status = x.Status.ToString(),
        CreatedAt = x.CreatedAt
    };
}