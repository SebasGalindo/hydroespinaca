using SensorService.Domain.Entities;

namespace SensorService.Domain.Interfaces;
public interface ISensorAlertRepository
{
    Task CreateAsync(SensorAlert alert);
    Task<List<SensorAlert>> GetByVariableCodeAsync(string variableCode);
    Task UpdateAsync(SensorAlert alert);
    Task<SensorAlert?> GetByIdAsync(string id);
    Task<SensorAlert?> GetActiveByVariableCodeAsync(string variableCode);
    Task<int> DeleteOlderThanAsync(DateTime cutoffDate);

    /// <summary>
    /// Get count of active (unresolved, unacknowledged) alerts for specific variables.
    /// </summary>
    /// <param name="variableCodes">List of variable codes to check for active alerts</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Count of active alerts matching the criteria</returns>
    Task<int> CountActiveAlertsByVariablesAsync(IEnumerable<string> variableCodes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Mark a sensor alert as email sent by setting the EmailSentAt timestamp.
    /// </summary>
    /// <param name="alertId">Alert ID to mark as sent</param>
    /// <param name="sentAt">Timestamp when email was sent</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task MarkEmailAsSentAsync(string alertId, DateTime sentAt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get active alerts (unresolved, unacknowledged) for variables that have NOT been emailed yet.
    /// Used to recover pending notifications after service restart.
    /// </summary>
    /// <param name="variableCodes">List of variable codes to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of alerts that need email notification</returns>
    Task<List<SensorAlert>> GetUnsentEmailAlertsByVariablesAsync(IEnumerable<string> variableCodes, CancellationToken cancellationToken = default);
}
