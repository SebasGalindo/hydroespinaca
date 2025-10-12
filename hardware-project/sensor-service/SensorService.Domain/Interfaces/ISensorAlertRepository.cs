using HydroEspinaca.Shared.Enums;
using SensorService.Domain.Entities;

namespace SensorService.Domain.Interfaces;
public interface ISensorAlertRepository
{
    Task CreateAsync(SensorAlert alert);
    Task<List<SensorAlert>> GetBySensorCodeAsync(string sensorCode);
    Task UpdateAsync(SensorAlert alert);
    Task<SensorAlert?> GetByIdAsync(string id);
    Task<SensorAlert?> GetUnacknowledgedBySensorAndTypeAsync(string sensorCode, AlertType type);
    Task<SensorAlert?> GetActiveBySensorVariableAndTypeAsync(string sensorCode, string variableCode, AlertType type);
    Task<int> DeleteOlderThanAsync(DateTime cutoffDate);

    /// <summary>
    /// Get count of active (unresolved, unacknowledged) alerts for specific sensors.
    /// Used by notification service to prevent spam after memory loss or service restart.
    /// </summary>
    /// <param name="sensorCodes">List of sensor codes to check for active alerts</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Count of active alerts matching the criteria</returns>
    Task<int> CountActiveAlertsBySensorsAsync(IEnumerable<string> sensorCodes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Mark a sensor alert as email sent by setting the EmailSentAt timestamp.
    /// </summary>
    /// <param name="alertId">Alert ID to mark as sent</param>
    /// <param name="sentAt">Timestamp when email was sent</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task MarkEmailAsSentAsync(string alertId, DateTime sentAt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get active alerts (unresolved, unacknowledged) for sensors that have NOT been emailed yet.
    /// Used to recover pending notifications after service restart.
    /// </summary>
    /// <param name="sensorCodes">List of sensor codes to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of alerts that need email notification</returns>
    Task<List<SensorAlert>> GetUnsentEmailAlertsBySensorsAsync(IEnumerable<string> sensorCodes, CancellationToken cancellationToken = default);
}
