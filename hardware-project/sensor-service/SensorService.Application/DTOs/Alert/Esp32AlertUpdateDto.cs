namespace SensorService.Application.DTOs.Alert;

/// <summary>
/// DTO para actualizar el estado de reconocimiento de una alerta de dispositivo ESP32.
/// Permite al usuario marcar una alerta de conectividad como reconocida.
/// </summary>
public class Esp32AlertUpdateDto
{
    /// <summary>Indica si la alerta del ESP32 ha sido reconocida por el usuario.</summary>
    public bool Acknowledged { get; set; }
}
