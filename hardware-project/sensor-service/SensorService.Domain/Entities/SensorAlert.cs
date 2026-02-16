namespace SensorService.Domain.Entities;

/// <summary>
/// Representa una alerta generada cuando el valor de una variable de sensor
/// se encuentra fuera del rango óptimo definido para el cultivo hidropónico.
/// </summary>
public class SensorAlert : AlertBase
{
    /// <summary>
    /// Código de la variable ambiental que generó la alerta (ej: ph, tds, water_level).
    /// </summary>
    public string VariableCode { get; set; } = default!;

    /// <summary>
    /// Valor de la lectura que originó la alerta al estar fuera del rango óptimo.
    /// </summary>
    public double Value { get; set; }

    /// <summary>
    /// Fecha y hora de la última vez que se detectó el valor fuera de rango.
    /// </summary>
    public DateTime LastSeen { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Último valor registrado de la variable, utilizado para actualizar alertas activas.
    /// </summary>
    public double? LatestValue { get; set; }

    /// <summary>
    /// Marca temporal de cuándo se envió el correo electrónico de alerta crítica.
    /// <c>null</c> indica que el correo aún no ha sido enviado.
    /// Se utiliza para prevenir el envío de correos duplicados.
    /// </summary>
    public DateTime? EmailSentAt { get; set; }
}