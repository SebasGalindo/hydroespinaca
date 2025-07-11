using SensorService.Application.DTOs;
using SensorService.Application.Interfaces;
using SensorService.Domain.Entities;
using SensorService.Domain.Interfaces;

namespace SensorService.Application.Services;

public class AggregateService : IAggregateService
{
    private readonly IAggregateRepository _repo;

    public AggregateService(IAggregateRepository repo)
    {
        _repo = repo;
    }

    public async Task<List<AggregateDto>> GetBySensorAndVariableAsync(string sensorId, string variableId, DateTime from, DateTime to)
    {
        var results = await _repo.GetBySensorAndVariableAsync(sensorId, variableId, from, to);
        return results.Select(x => new AggregateDto
        {
            SensorId = x.SensorId,
            VariableId = x.VariableId,
            Avg = x.Avg,
            Min = x.Min,
            Max = x.Max,
            Count = x.Count,
            Timestamp = x.Timestamp
        }).ToList();
    }


    public async Task SaveAsync(AggregateDto dto)
    {
        var agg = new Aggregate
        {
            SensorId = dto.SensorId,
            VariableId = dto.VariableId,
            Avg = dto.Avg,
            Min = dto.Min,
            Max = dto.Max,
            Count = dto.Count,
            Timestamp = dto.Timestamp
        };

        await _repo.CreateAsync(agg);
    }
}
