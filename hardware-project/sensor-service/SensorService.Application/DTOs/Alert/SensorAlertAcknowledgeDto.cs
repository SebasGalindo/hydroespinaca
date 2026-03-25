namespace SensorService.Application.DTOs.Alert;

/// <summary>
/// DTO para actualizar el estado de reconocimiento de una alerta de sensor.
/// Permite al usuario marcar una alerta como reconocida o no reconocida.
/// </summary>
public class SensorAlertUpdateDto
{
    /// <summary>Indica si la alerta ha sido reconocida por el usuario.</summary>
    public bool Acknowledged { get; set; }
}

