using SensorService.Application.DTOs;

namespace SensorService.Application.Interfaces;

public interface IAggregateService
{
    Task<List<AggregateDto>> GetBySensorAndVariableAsync(string sensorId, string variableId, DateTime from, DateTime to);
    Task SaveAsync(AggregateDto dto);
}
