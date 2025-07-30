using ActuatorService.Domain.Entities;

namespace ActuatorService.Domain.Interfaces;

public interface ICommandLogRepository
{
    Task AddAsync(ActuatorCommand command);
    Task<List<ActuatorCommand>> GetByActuatorIdAsync(string actuatorId);
    Task<List<ActuatorCommand>> GetByDateRangeAsync(DateTime from, DateTime to);
}
