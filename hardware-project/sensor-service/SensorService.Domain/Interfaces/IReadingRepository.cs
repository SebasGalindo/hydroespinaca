using SensorService.Domain.Entities;

namespace SensorService.Domain.Interfaces;

public interface IReadingRepository
{
    Task CreateAsync(Reading reading);
    Task<List<Reading>> GetBySensorAndVariableAsync(string sensorCode, string variableCode, DateTime from, DateTime to);
    Task<int> DeleteOlderThanAsync(DateTime cutoff);
    Task<Reading?> GetLatestBySensorCodesAsync(List<string> sensorCodes);
    Task<List<Reading>> GetLatestReadingsByVariableAsync();
}
