using ActuatorService.Application.DTOs;

namespace ActuatorService.Application.Interfaces;
public interface IActuatorService
{
    Task<List<ActuatorDto>> GetAllAsync();
    Task<ActuatorDto?> GetByIdAsync(string id);
    Task<List<ActuatorDto>> GetByEsp32IdAsync(string esp32Id);
    Task<string> CreateAsync(CreateActuatorDto dto);
    Task UpdateAsync(UpdateActuatorDto dto);
    Task DeleteAsync(string id);
}