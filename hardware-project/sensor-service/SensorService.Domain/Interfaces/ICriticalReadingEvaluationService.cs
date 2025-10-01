using SensorService.Domain.Entities;
using SensorService.Domain.ValueObjects;

namespace SensorService.Domain.Interfaces;

public interface ICriticalReadingEvaluationService
{
    Task<CriticalAlertData> EvaluateCriticalReadingsAsync(string esp32Id, DateTime timestamp, IEnumerable<Reading> readings, IEnumerable<Sensor> sensors, CancellationToken cancellationToken = default);
}