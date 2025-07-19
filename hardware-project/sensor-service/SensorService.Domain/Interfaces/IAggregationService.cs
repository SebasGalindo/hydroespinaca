using SensorService.Domain.Entities;
using SensorService.Domain.ValueObjects;

namespace SensorService.Domain.Interfaces;

public interface IAggregationService
{
    Aggregate CreateAggregate(string sensorId, string variableId, TimeWindow window, AggregateData data);
}