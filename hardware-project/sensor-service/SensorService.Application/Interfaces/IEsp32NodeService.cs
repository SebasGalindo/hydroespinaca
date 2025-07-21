using HydroEspinaca.Shared.DTOs.Esp32;

namespace SensorService.Application.Interfaces;

public interface IEsp32NodeService
{
    Task<List<Esp32NodeDto>> GetAllAsync();
    Task<Esp32NodeDto?> GetByIdAsync(string id);
    Task CreateAsync(Esp32NodeCreateDto dto);
    Task UpdateStatusAsync(Esp32NodeUpdateStatusDto dto);
}