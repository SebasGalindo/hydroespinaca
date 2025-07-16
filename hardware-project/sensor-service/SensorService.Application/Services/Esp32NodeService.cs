using SensorService.Application.DTOs.Esp32Node;
using SensorService.Application.Interfaces;
using SensorService.Domain.Entities;
using SensorService.Domain.Interfaces;

namespace SensorService.Application.Services;

public class Esp32NodeService : IEsp32NodeService
{
    private readonly IEsp32NodeRepository _repo;

    public Esp32NodeService(IEsp32NodeRepository repo)
    {
        _repo = repo;
    }

    public async Task<List<Esp32NodeDto>> GetAllAsync()
    {
        var nodes = await _repo.GetAllAsync();
        return nodes.Select(x => new Esp32NodeDto
        {
            Id = x.Id,
            Name = x.Name,
            Location = x.Location,
            LastSeen = x.LastSeen,
            Status = x.Status
        }).ToList();
    }

    public async Task<Esp32NodeDto?> GetByIdAsync(string id)
    {
        var node = await _repo.GetByIdAsync(id);
        return node is null ? null : new Esp32NodeDto
        {
            Id = node.Id,
            Name = node.Name,
            Location = node.Location,
            LastSeen = node.LastSeen,
            Status = node.Status
        };
    }

    public async Task CreateAsync(Esp32NodeCreateDto dto)
    {
        var exists = await _repo.GetByIdAsync(dto.Id);
        if (exists != null)
            throw new InvalidOperationException($"El ESP32 con ID '{dto.Id}' ya existe.");

        var entity = new Esp32Node
        {
            Id = dto.Id,
            Name = dto.Name,
            Location = dto.Location,
            LastSeen = DateTime.UtcNow,
            Status = "active"
        };

        await _repo.CreateAsync(entity);
    }

    public async Task UpdateStatusAsync(string id, Esp32NodeUpdateStatusDto dto)
    {
        var node = await _repo.GetByIdAsync(id);
        if (node is null)
            throw new InvalidOperationException($"ESP32 '{id}' no encontrado.");

        node.Status = dto.Status;
        await _repo.UpdateStatusAsync(id, node.Status);
    }
}
