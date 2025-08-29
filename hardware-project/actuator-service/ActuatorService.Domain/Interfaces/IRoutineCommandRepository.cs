using ActuatorService.Domain.Entities;

namespace ActuatorService.Domain.Interfaces;

public interface IRoutineCommandRepository
{
    Task<RoutineCommand?> GetByCommandIdAsync(string commandId);
    Task<List<RoutineCommand>> GetByRoutineIdAsync(string routineId);
    Task AddAsync(RoutineCommand routineCommand);
    Task UpdateAsync(RoutineCommand routineCommand);
    Task DeleteAsync(string id);
    Task<long> DeleteOlderThanAsync(DateTime cutoffDate);
}