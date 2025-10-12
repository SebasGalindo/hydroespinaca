using SensorService.Domain.Entities;
using SensorService.Domain.ValueObjects;

namespace SensorService.Domain.Interfaces;

public interface IAggregationService
{
    Aggregate CreateAggregate(string sensorCode, string variableCode, TimeWindow window, AggregateData data);
}