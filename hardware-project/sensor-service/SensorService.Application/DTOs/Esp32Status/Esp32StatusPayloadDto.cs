using System.Text.Json.Serialization;

namespace SensorService.Application.DTOs.Esp32Status;

/// <summary>
/// DTO que representa el payload de estado recibido desde un dispositivo ESP32 vía MQTT.
/// Contiene información de telemetría como memoria libre, tiempo de actividad y estado de conexión.
/// </summary>
public class Esp32StatusPayloadDto
{
    /// <summary>Estado reportado por el ESP32 (online, offline, running).</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = default!;

    /// <summary>Marca de tiempo del reporte de estado.</summary>
    [JsonPropertyName("timestamp")]
    public DateTime? Timestamp { get; set; }

    /// <summary>Memoria heap libre del ESP32 en bytes.</summary>
    [JsonPropertyName("freeHeap")]
    public long? FreeHeap { get; set; }

    /// <summary>Tiempo de actividad del ESP32 en segundos desde el último reinicio.</summary>
    [JsonPropertyName("uptime")]
    public long? Uptime { get; set; }

    /// <summary>Identificador del ESP32, derivado del tópico MQTT (no del payload).</summary>
    public string Esp32Id { get; set; } = string.Empty;

    /// <summary>Indica si el ESP32 está en línea.</summary>
    public bool IsOnline => string.Equals(Status, "online", StringComparison.OrdinalIgnoreCase);
    /// <summary>Indica si el ESP32 está fuera de línea.</summary>
    public bool IsOffline => string.Equals(Status, "offline", StringComparison.OrdinalIgnoreCase);
    /// <summary>Indica si el ESP32 está en estado de ejecución activa.</summary>
    public bool IsRunning => string.Equals(Status, "running", StringComparison.OrdinalIgnoreCase);
    /// <summary>Indica si el estado reportado es un valor válido reconocido por el sistema.</summary>
    public bool IsValidStatus => IsOnline || IsOffline || IsRunning;
}