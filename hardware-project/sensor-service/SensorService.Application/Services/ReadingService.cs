using SensorService.Application.DTOs.Reading;
using SensorService.Application.Interfaces;
using SensorService.Application.Mappers;
using SensorService.Domain.Interfaces;

namespace SensorService.Application.Services;

public class ReadingService : IReadingService
{
    private readonly IReadingRepository _repo;

    public ReadingService(IReadingRepository repo)
    {
        _repo = repo;
    }

    public async Task<List<ReadingDto>> GetBySensorAndVariableAsync(string sensorId, string variableId, DateTime from, DateTime to)
    {
        var list = await _repo.GetBySensorAndVariableAsync(sensorId, variableId, from, to);
        return list.Select(ReadingMapper.ToDto).ToList();
    }
}