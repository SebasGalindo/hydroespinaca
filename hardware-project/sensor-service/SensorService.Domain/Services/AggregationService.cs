using SensorService.Domain.Entities;
using SensorService.Domain.Interfaces;
using SensorService.Domain.ValueObjects;

namespace SensorService.Domain.Services;

public class AggregationService : IAggregationService
{
    public Aggregate CreateAggregate(
        string sensorCode,
        string variableCode,
        TimeWindow window,
        AggregateData data)
    {
        var aggregateId = GenerateAggregateId(sensorCode, variableCode, window.End);

        var aggregate = new Aggregate
        {
            SensorCode = sensorCode,
            VariableCode = variableCode,
            Avg = data.Average,
            Min = data.Min,
            Max = data.Max,
            Count = data.Count,
            Timestamp = window.End
        };
        aggregate.SetId(aggregateId);
        return aggregate;

    }

    private static string GenerateAggregateId(string sensorCode, string variableCode, DateTime timestamp)
    {
        return $"{sensorCode}-{variableCode}-{timestamp:yyyyMMddHHmm}";
    }
}