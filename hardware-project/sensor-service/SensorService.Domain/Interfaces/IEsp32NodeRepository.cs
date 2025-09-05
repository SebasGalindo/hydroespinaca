using HydroEspinaca.Shared.Enums;
using SensorService.Domain.Entities;

namespace SensorService.Domain.Interfaces;

public interface IEsp32NodeRepository
{
    Task<List<Esp32Node>> GetAllAsync();
    Task<Esp32Node?> GetByIdAsync(string id);
    Task CreateAsync(Esp32Node node);
    Task UpdateAsync(Esp32Node node);
    Task<bool> UpdateStatusAsync(string id, Esp32Status status);
    Task<bool> ExistsAsync(string id);
    Task<bool> UpdateLastSeenAsync(string id, DateTime lastSeen);
}