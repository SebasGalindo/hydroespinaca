using HydroEspinaca.Shared.DTOs.Actuator;

namespace ActuatorService.Application.Interfaces;

public interface IControlOutputService
{
    Task<ControlOutputDto> GetByIdAsync(string id);
    Task<List<ControlOutputDto>> GetAllAsync();
    Task<List<ControlOutputDto>> GetByActuatorIdAsync(string actuatorId);
    Task<string> AddAsync(CreateControlOutputDto dto);
    Task UpdateAsync(string id, UpdateControlOutputDto dto);
    Task DeleteAsync(string id);
}
