namespace SensorService.Domain.Entities;

/// <summary>
/// Representa una alerta generada cuando un nodo ESP32 se desconecta
/// del sistema o deja de reportar datos.
/// </summary>
public class Esp32Alert : AlertBase
{
    /// <summary>
    /// Identificador del nodo ESP32 que generó la alerta de desconexión.
    /// </summary>
    public string Esp32Id { get; set; } = default!;

    /// <summary>
    /// Marca temporal de cuándo se envió el correo electrónico de alerta de desconexión.
    /// <c>null</c> indica que el correo aún no ha sido enviado.
    /// Se utiliza para prevenir el envío de correos duplicados.
    /// </summary>
    public DateTime? EmailSentAt { get; set; }
}
