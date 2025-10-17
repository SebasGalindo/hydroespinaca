using SensorService.Application.DTOs.Aggregate;

namespace SensorService.Application.Interfaces;

public interface IAggregateService
{
    Task<List<AggregateDto>> GetBySensorAndVariableAsync(string sensorId, string variableId, DateTime from, DateTime to);
    Task<EnvironmentalAggregatesResponse> GetEnvironmentalAggregatesAsync(GetEnvironmentalAggregatesRequest request);
}
