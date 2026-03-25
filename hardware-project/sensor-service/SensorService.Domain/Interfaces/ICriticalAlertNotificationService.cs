using SensorService.Domain.ValueObjects;

namespace SensorService.Domain.Interfaces;

/// <summary>
/// Servicio de notificación de alertas críticas de lecturas de sensores.
/// Gestiona el envío de notificaciones por correo electrónico cuando los valores
/// de variables de regulación manual están fuera de rango.
/// </summary>
public interface ICriticalAlertNotificationService
{
    /// <summary>
    /// Envía una notificación de alerta crítica con los datos consolidados de las lecturas.
    /// </summary>
    /// <param name="alertData">Datos consolidados de la alerta crítica incluyendo lecturas manuales y contextuales.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task SendCriticalAlertAsync(CriticalAlertData alertData, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determina si se debe enviar una notificación de alerta para las variables especificadas,
    /// verificando que no se haya enviado previamente.
    /// </summary>
    /// <param name="variableCodes">Códigos de las variables a verificar.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns><c>true</c> si se debe enviar la alerta; de lo contrario, <c>false</c>.</returns>
    Task<bool> ShouldSendAlertAsync(IEnumerable<string> variableCodes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marca las alertas de las variables especificadas como enviadas para evitar duplicados.
    /// </summary>
    /// <param name="variableCodes">Códigos de las variables cuyas alertas se marcan como enviadas.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task MarkAlertAsSentAsync(IEnumerable<string> variableCodes, CancellationToken cancellationToken = default);
}