using SensorService.Domain.Entities;

namespace SensorService.Domain.Interfaces;

public interface ISensorReadingRepository
{
    Task SaveAsync(SensorReading data);
}
