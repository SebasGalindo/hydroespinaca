using SensorService.Domain.Entities;

namespace SensorService.Domain.Interfaces;

public interface IEsp32NodeRepository
{
    Task<List<Esp32Node>> GetAllAsync();
    Task<Esp32Node?> GetByIdAsync(string id);
    Task CreateAsync(Esp32Node node);
    Task UpdateStatusAsync(string id, string status);
    Task<bool> ExistsAsync(string id);

}