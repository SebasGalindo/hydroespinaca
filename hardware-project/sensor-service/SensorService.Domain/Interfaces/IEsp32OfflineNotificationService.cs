using SensorService.Domain.Entities;

namespace SensorService.Domain.Interfaces;

/// <summary>
/// Servicio de notificación de alertas de desconexión de nodos ESP32.
/// Gestiona el envío de correos electrónicos cuando un nodo se desconecta del sistema.
/// </summary>
public interface IEsp32OfflineNotificationService
{
    /// <summary>
    /// Envía una notificación por correo electrónico informando la desconexión de un nodo ESP32.
    /// </summary>
    /// <param name="alert">Alerta de desconexión con los datos del nodo offline.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task SendOfflineAlertAsync(Esp32Alert alert, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determina si se debe enviar una notificación de desconexión para el nodo ESP32 especificado,
    /// verificando que no se haya enviado previamente.
    /// </summary>
    /// <param name="esp32Id">Identificador del nodo ESP32.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns><c>true</c> si se debe enviar la alerta; de lo contrario, <c>false</c>.</returns>
    Task<bool> ShouldSendAlertAsync(string esp32Id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marca la alerta de desconexión como enviada para evitar notificaciones duplicadas.
    /// </summary>
    /// <param name="alertId">Identificador de la alerta a marcar como enviada.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task MarkAlertAsSentAsync(string alertId, CancellationToken cancellationToken = default);
}
