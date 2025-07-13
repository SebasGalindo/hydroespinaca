using SensorService.Domain.Entities;

namespace SensorService.Domain.Interfaces;

public interface IReadingRepository
{
    Task CreateAsync(Reading reading);
    Task<List<Reading>> GetBySensorAndVariableAsync(string sensorId, string variableId, DateTime from, DateTime to);
    Task<int> DeleteOlderThanAsync(DateTime cutoff);
}
