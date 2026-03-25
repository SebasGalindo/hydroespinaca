using SensorService.Domain.Entities;

namespace SensorService.Domain.Interfaces;

/// <summary>
/// Repositorio para la persistencia y consulta de alertas generadas por lecturas
/// de sensores fuera del rango óptimo.
/// </summary>
public interface ISensorAlertRepository
{
    /// <summary>
    /// Crea una nueva alerta de sensor en la base de datos.
    /// </summary>
    /// <param name="alert">Alerta de sensor a persistir.</param>
    Task CreateAsync(SensorAlert alert);

    /// <summary>
    /// Obtiene todas las alertas asociadas a un código de variable específico.
    /// </summary>
    /// <param name="variableCode">Código de la variable ambiental.</param>
    /// <returns>Lista de alertas para la variable especificada.</returns>
    Task<List<SensorAlert>> GetByVariableCodeAsync(string variableCode);

    /// <summary>
    /// Actualiza una alerta de sensor existente en la base de datos.
    /// </summary>
    /// <param name="alert">Alerta de sensor con los datos actualizados.</param>
    Task UpdateAsync(SensorAlert alert);

    /// <summary>
    /// Obtiene una alerta de sensor por su identificador único.
    /// </summary>
    /// <param name="id">Identificador único de la alerta.</param>
    /// <returns>La alerta encontrada o <c>null</c> si no existe.</returns>
    Task<SensorAlert?> GetByIdAsync(string id);

    /// <summary>
    /// Obtiene la alerta activa (no resuelta ni reconocida) para una variable específica.
    /// </summary>
    /// <param name="variableCode">Código de la variable ambiental.</param>
    /// <returns>La alerta activa o <c>null</c> si no existe ninguna.</returns>
    Task<SensorAlert?> GetActiveByVariableCodeAsync(string variableCode);

    /// <summary>
    /// Elimina alertas anteriores a la fecha de corte especificada para limpieza periódica.
    /// </summary>
    /// <param name="cutoffDate">Fecha límite; se eliminan alertas anteriores a esta fecha.</param>
    /// <returns>Cantidad de alertas eliminadas.</returns>
    Task<int> DeleteOlderThanAsync(DateTime cutoffDate);

    /// <summary>
    /// Obtiene la cantidad de alertas activas (no resueltas ni reconocidas) para las variables especificadas.
    /// </summary>
    /// <param name="variableCodes">Códigos de las variables a verificar.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Cantidad de alertas activas que coinciden con los criterios.</returns>
    Task<int> CountActiveAlertsByVariablesAsync(IEnumerable<string> variableCodes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marca una alerta de sensor como notificada por correo electrónico registrando la fecha de envío.
    /// </summary>
    /// <param name="alertId">Identificador de la alerta a marcar.</param>
    /// <param name="sentAt">Fecha y hora del envío del correo.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task MarkEmailAsSentAsync(string alertId, DateTime sentAt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene alertas activas (no resueltas ni reconocidas) de variables que aún no han sido notificadas por correo.
    /// Se utiliza para recuperar notificaciones pendientes tras un reinicio del servicio.
    /// </summary>
    /// <param name="variableCodes">Códigos de las variables a verificar.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Lista de alertas pendientes de notificación por correo electrónico.</returns>
    Task<List<SensorAlert>> GetUnsentEmailAlertsByVariablesAsync(IEnumerable<string> variableCodes, CancellationToken cancellationToken = default);
}
