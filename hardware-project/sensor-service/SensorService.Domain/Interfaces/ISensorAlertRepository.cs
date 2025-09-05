using HydroEspinaca.Shared.Enums;
using SensorService.Domain.Entities;

namespace SensorService.Domain.Interfaces;
public interface ISensorAlertRepository
{
    Task CreateAsync(SensorAlert alert);
    Task<List<SensorAlert>> GetBySensorIdAsync(string sensorId);
    Task UpdateAsync(SensorAlert alert);
    Task<SensorAlert?> GetByIdAsync(string id);
    Task<SensorAlert?> GetUnacknowledgedBySensorAndTypeAsync(string sensorId, AlertType type);
    Task<int> DeleteOlderThanAsync(DateTime cutoffDate);
}
