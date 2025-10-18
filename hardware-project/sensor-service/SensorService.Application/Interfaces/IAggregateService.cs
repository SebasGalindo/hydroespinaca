using SensorService.Application.DTOs.Aggregate;
using HydroEspinaca.Shared.DTOs.Analytics;

namespace SensorService.Application.Interfaces;

public interface IAggregateService
{
    Task<List<AggregateDto>> GetBySensorAndVariableAsync(string sensorId, string variableId, DateTime from, DateTime to);
    Task<EnvironmentalAggregatesResponse> GetEnvironmentalAggregatesAsync(EnvironmentalAnalyticsRequest request);
}
