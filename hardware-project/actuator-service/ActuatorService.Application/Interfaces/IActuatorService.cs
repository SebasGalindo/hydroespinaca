using HydroEspinaca.Shared.DTOs.Actuator;

namespace ActuatorService.Application.Interfaces;
/// <summary>
/// Contract for the actuator application service handling CRUD operations and analytics.
/// </summary>
public interface IActuatorService
{
    Task<ActuatorDto> GetByIdAsync(string id);
    Task<List<ActuatorDto>> GetAllAsync();
    Task<List<ActuatorDto>> GetByEsp32IdAsync(string esp32Id);
    Task<string> AddAsync(CreateActuatorDto dto);
    Task UpdateAsync(string id, UpdateActuatorDto dto);
    Task DeleteAsync(string id);
}
