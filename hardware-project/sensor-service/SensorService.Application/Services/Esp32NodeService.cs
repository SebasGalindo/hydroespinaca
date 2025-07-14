using SensorService.Application.DTOs;
using SensorService.Application.Interfaces;
using SensorService.Domain.Entities;
using SensorService.Domain.Interfaces;

namespace SensorService.Application.Services;

public class Esp32NodeService : IEsp32NodeService
{
    private readonly IEsp32NodeRepository _repository;

    public Esp32NodeService(IEsp32NodeRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<Esp32NodeDto>> GetAllAsync() =>
        (await _repository.GetAllAsync()).Select(n => new Esp32NodeDto
        {
            Id = n.Id,
            Name = n.Name,
            Location = n.Location,
            LastSeen = n.LastSeen,
            Status = n.Status
        }).ToList();

    public async Task<Esp32NodeDto?> GetByIdAsync(string id)
    {
        var node = await _repository.GetByIdAsync(id);
        if (node == null) return null;
        return new Esp32NodeDto
        {
            Id = node.Id,
            Name = node.Name,
            Location = node.Location,
            LastSeen = node.LastSeen,
            Status = node.Status
        };
    }

    public async Task CreateAsync(Esp32NodeDto dto)
    {
        var entity = new Esp32Node
        {
            Id = dto.Id,
            Name = dto.Name,
            Location = dto.Location,
            LastSeen = dto.LastSeen,
            Status = dto.Status
        };

        await _repository.CreateAsync(entity);
    }

    public async Task UpdateStatusAsync(string id, string status)
    {
        await _repository.UpdateStatusAsync(id, status);
    }
}
