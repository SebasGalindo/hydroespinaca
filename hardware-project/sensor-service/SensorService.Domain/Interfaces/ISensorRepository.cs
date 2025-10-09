namespace SensorService.Domain.Interfaces;

using SensorService.Domain.Entities;

public interface ISensorRepository
{
    Task<Sensor?> GetByIdAsync(string id);
    Task<List<Sensor>> GetAllAsync();
    Task<List<Sensor>> GetSensorsByEsp32IdAsync(string esp32Id, CancellationToken cancellationToken = default);
    Task CreateAsync(Sensor sensor);
    Task UpdateAsync(Sensor sensor);
    Task DeleteAsync(string id);
}
