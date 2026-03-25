namespace SensorService.Application.DTOs.Esp32Node;

/// <summary>
/// Resultado de la verificación periódica del estado de conectividad de los dispositivos ESP32.
/// </summary>
/// <param name="TotalEsp32Checked">Cantidad total de dispositivos ESP32 verificados.</param>
/// <param name="OfflineEsp32Count">Cantidad de dispositivos ESP32 detectados como desconectados.</param>
/// <param name="AlertsCreated">Cantidad de alertas de desconexión generadas.</param>
public record CheckEsp32StatusResult(
    int TotalEsp32Checked,
    int OfflineEsp32Count,
    int AlertsCreated);

/// <summary>
/// Información del estado de conectividad de un dispositivo ESP32 individual.
/// </summary>
/// <param name="Esp32Id">Identificador único del dispositivo ESP32.</param>
/// <param name="LastActivity">Última fecha y hora de actividad registrada.</param>
/// <param name="IsOffline">Indica si el dispositivo se considera desconectado.</param>
/// <param name="MinutesSinceLastActivity">Minutos transcurridos desde la última actividad.</param>
public record Esp32StatusInfo(
    string Esp32Id,
    DateTime LastActivity,
    bool IsOffline,
    double MinutesSinceLastActivity);