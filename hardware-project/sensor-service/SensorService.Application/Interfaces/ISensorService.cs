using HydroEspinaca.Shared.DTOs.Sensors;

namespace SensorService.Application.Interfaces;
public interface ISensorService
{
    Task<List<SensorDto>> GetAllAsync();
    Task<SensorDto?> GetByIdAsync(string id);
    Task<string> CreateAsync(SensorCreateDto dto);
    Task UpdateAsync(SensorUpdateDto dto);
    Task DeleteAsync(string id);
}
