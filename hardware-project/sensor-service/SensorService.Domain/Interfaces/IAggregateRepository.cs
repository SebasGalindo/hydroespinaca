using SensorService.Domain.Entities;

namespace SensorService.Domain.Interfaces;

public interface IAggregateRepository
{
    Task CreateAsync(Aggregate aggregate);
    Task<List<Aggregate>> GetBySensorAndVariableAsync(string sensorCode, string variableCode, DateTime from, DateTime to);
    Task<Aggregate?> GetBySensorAndVariableAndTimestampAsync(string sensorCode, string variableCode, DateTime timestamp);
}
