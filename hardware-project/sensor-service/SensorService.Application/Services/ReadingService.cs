using SensorService.Application.DTOs;
using SensorService.Application.Interfaces;
using SensorService.Domain.Entities;
using SensorService.Domain.Interfaces;

namespace SensorService.Application.Services;

public class ReadingService : IReadingService
{
    private readonly IReadingRepository _repo;

    public ReadingService(IReadingRepository repo)
    {
        _repo = repo;
    }

    public async Task AddAsync(ReadingDto dto)
    {
        var reading = new Reading
        {
            SensorId = dto.SensorId,
            VariableId = dto.VariableId,
            Value = dto.Value,
            Timestamp = dto.Timestamp
        };

        await _repo.CreateAsync(reading);
    }

    public async Task<List<ReadingDto>> GetBySensorAndVariableAsync(string sensorId, string variableId, DateTime from, DateTime to)
    {
        var list = await _repo.GetBySensorAndVariableAsync(sensorId, variableId, from, to);
        return list.Select(x => new ReadingDto
        {
            SensorId = x.SensorId,
            VariableId = x.VariableId,
            Value = x.Value,
            Timestamp = x.Timestamp
        }).ToList();
    }

}