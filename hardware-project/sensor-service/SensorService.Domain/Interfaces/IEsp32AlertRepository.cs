using SensorService.Domain.Entities;

namespace SensorService.Domain.Interfaces;

/// <summary>
/// Repositorio para la persistencia y consulta de alertas de desconexión de nodos ESP32.
/// </summary>
public interface IEsp32AlertRepository
{
    /// <summary>
    /// Obtiene la alerta activa (no resuelta) de un nodo ESP32 específico.
    /// </summary>
    /// <param name="esp32Id">Identificador del nodo ESP32.</param>
    /// <returns>La alerta activa o <c>null</c> si no existe ninguna.</returns>
    Task<Esp32Alert?> GetActiveByEsp32IdAsync(string esp32Id);

    /// <summary>
    /// Crea una nueva alerta de desconexión en la base de datos.
    /// </summary>
    /// <param name="alert">Alerta de ESP32 a persistir.</param>
    Task CreateAsync(Esp32Alert alert);

    /// <summary>
    /// Actualiza una alerta de desconexión existente en la base de datos.
    /// </summary>
    /// <param name="alert">Alerta de ESP32 con los datos actualizados.</param>
    Task UpdateAsync(Esp32Alert alert);

    /// <summary>
    /// Obtiene una alerta de ESP32 por su identificador único.
    /// </summary>
    /// <param name="id">Identificador único de la alerta.</param>
    /// <returns>La alerta encontrada o <c>null</c> si no existe.</returns>
    Task<Esp32Alert?> GetByIdAsync(string id);

    /// <summary>
    /// Obtiene todas las alertas (activas y resueltas) de un nodo ESP32 específico.
    /// </summary>
    /// <param name="esp32Id">Identificador del nodo ESP32.</param>
    /// <returns>Lista de alertas del nodo ESP32.</returns>
    Task<List<Esp32Alert>> GetByEsp32IdAsync(string esp32Id);

    /// <summary>
    /// Elimina alertas anteriores a la fecha de corte especificada para limpieza periódica.
    /// </summary>
    /// <param name="cutoffDate">Fecha límite; se eliminan alertas anteriores a esta fecha.</param>
    /// <returns>Cantidad de alertas eliminadas.</returns>
    Task<int> DeleteOlderThanAsync(DateTime cutoffDate);

    /// <summary>
    /// Obtiene alertas activas de nodos ESP32 que aún no han sido notificadas por correo electrónico.
    /// Se utiliza para recuperar notificaciones pendientes tras un reinicio del servicio.
    /// </summary>
    /// <param name="esp32Ids">Identificadores de los nodos ESP32 a verificar.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Lista de alertas pendientes de notificación por correo.</returns>
    Task<List<Esp32Alert>> GetUnsentEmailAlertsByEsp32IdsAsync(IEnumerable<string> esp32Ids, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marca una alerta como notificada por correo electrónico registrando la fecha de envío.
    /// </summary>
    /// <param name="alertId">Identificador de la alerta a marcar.</param>
    /// <param name="sentAt">Fecha y hora del envío del correo.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task MarkEmailAsSentAsync(string alertId, DateTime sentAt, CancellationToken cancellationToken = default);
}
