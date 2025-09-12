using System.Text.Json.Serialization;

namespace SensorService.Application.DTOs.Esp32Status;

public class Esp32StatusPayloadDto
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = default!;

    [JsonPropertyName("timestamp")]
    public DateTime? Timestamp { get; set; }

    [JsonPropertyName("freeHeap")]
    public long? FreeHeap { get; set; }

    [JsonPropertyName("uptime")]
    public long? Uptime { get; set; }

    // ESP32 ID is derived from MQTT topic, not from payload
    public string Esp32Id { get; set; } = string.Empty;

    public bool IsOnline => string.Equals(Status, "online", StringComparison.OrdinalIgnoreCase);
    public bool IsOffline => string.Equals(Status, "offline", StringComparison.OrdinalIgnoreCase);
    public bool IsRunning => string.Equals(Status, "running", StringComparison.OrdinalIgnoreCase);
    public bool IsValidStatus => IsOnline || IsOffline || IsRunning;
}