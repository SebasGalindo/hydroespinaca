using HydroEspinaca.Shared.DTOs.Sensors;

namespace SensorService.Application.Interfaces;
public interface ISensorService
{
    Task<List<SensorDto>> GetAllAsync();
    Task<SensorDto?> GetByIdAsync(string id);
    Task<SensorDto> CreateAsync(SensorCreateDto dto);
    Task UpdateAsync(string id, SensorUpdateDto dto);
    Task DeleteAsync(string id);
}
