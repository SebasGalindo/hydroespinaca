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
    Task<SensorAlert?> GetActiveBySensorVariableAndTypeAsync(string sensorId, string variableId, AlertType type);
    Task<int> DeleteOlderThanAsync(DateTime cutoffDate);

    /// <summary>
    /// Get count of active (unresolved, unacknowledged) alerts for specific sensors.
    /// Used by notification service to prevent spam after memory loss or service restart.
    /// </summary>
    /// <param name="sensorIds">List of sensor IDs to check for active alerts</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Count of active alerts matching the criteria</returns>
    Task<int> CountActiveAlertsBySensorsAsync(IEnumerable<string> sensorIds, CancellationToken cancellationToken = default);
}
