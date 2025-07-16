using SensorService.Application.DTOs.Reading;

namespace SensorService.Application.Interfaces;

public interface IReadingService
{
    Task AddAsync(ReadingDto dto);
    Task<List<ReadingDto>> GetBySensorAndVariableAsync(string sensorId, string variableId, DateTime from, DateTime to);

}
