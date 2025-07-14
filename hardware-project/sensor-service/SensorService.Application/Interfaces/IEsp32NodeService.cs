using SensorService.Application.DTOs;

namespace SensorService.Application.Interfaces;

public interface IEsp32NodeService
{
    Task<List<Esp32NodeDto>> GetAllAsync();
    Task<Esp32NodeDto?> GetByIdAsync(string id);
    Task CreateAsync(Esp32NodeDto dto);
    Task UpdateStatusAsync(string id, string status);
}