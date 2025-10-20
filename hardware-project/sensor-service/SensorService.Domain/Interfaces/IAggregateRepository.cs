using SensorService.Domain.Entities;
using HydroEspinaca.Shared.DTOs.Analytics;

namespace SensorService.Domain.Interfaces;

public interface IAggregateRepository
{
    Task CreateAsync(Aggregate aggregate);
    Task<List<Aggregate>> GetBySensorAndVariableAsync(string sensorCode, string variableCode, DateTime from, DateTime to);
    Task<Aggregate?> GetBySensorAndVariableAndTimestampAsync(string sensorCode, string variableCode, DateTime timestamp);
    Task<Dictionary<string, List<Aggregate>>> GetEnvironmentalAggregatesAsync(EnvironmentalAnalyticsRequest request);
}
