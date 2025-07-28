using HydroEspinaca.Shared.Abstractions;
using SensorService.Domain.Entities;
using SensorService.Domain.Interfaces;
using SensorService.Domain.ValueObjects;

namespace SensorService.Domain.Services;

public class AggregationService : IAggregationService
{
    public Aggregate CreateAggregate(
        string sensorId,
        string variableId,
        TimeWindow window,
        AggregateData data)
    {
        var aggregateId = GenerateAggregateId(sensorId, variableId, window.End);

        var aggregate = new Aggregate
        {
            SensorId = sensorId,
            VariableId = variableId,
            Avg = data.Average,
            Min = data.Min,
            Max = data.Max,
            Count = data.Count,
            Timestamp = window.End
        };
        aggregate.SetId(aggregateId);
        return aggregate;

    }

    private static string GenerateAggregateId(string sensorId, string variableId, DateTime timestamp)
    {
        return $"{sensorId}-{variableId}-{timestamp:yyyyMMddHHmm}";
    }
}