using SensorService.Domain.Entities;

namespace SensorService.Domain.Interfaces;

public interface IAggregateRepository
{
    Task CreateAsync(Aggregate aggregate);
    Task<List<Aggregate>> GetBySensorAndVariableAsync(string sensorId, string variableId, DateTime from, DateTime to);
    Task<Aggregate?> GetBySensorAndVariableAndTimestampAsync(string sensorId, string variableId, DateTime timestamp);

}
