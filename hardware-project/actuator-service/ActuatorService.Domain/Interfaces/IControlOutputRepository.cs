using ActuatorService.Domain.Entities;

namespace ActuatorService.Domain.Interfaces;

public interface IControlOutputRepository
{
    Task<ControlOutput?> GetByIdAsync(string id);
    Task<List<ControlOutput>> GetAllAsync();
    Task<List<ControlOutput>> GetByActuatorIdAsync(string actuatorId);
    Task AddAsync(ControlOutput controlOutput);
    Task UpdateAsync(ControlOutput controlOutput);
    Task DeleteAsync(string id);
}
