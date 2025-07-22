using ActuatorService.Domain.Entities;

namespace ActuatorService.Domain.Interfaces;

public interface IActuatorRepository
{
    Task<Actuator?> GetByIdAsync(string id);
    Task<List<Actuator>> GetAllAsync();
    Task<List<Actuator>> GetByEsp32IdAsync(string esp32Id);
    Task AddAsync(Actuator actuator);
    Task UpdateAsync(Actuator actuator);
    Task DeleteAsync(string id);
}
