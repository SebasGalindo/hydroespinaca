using System.Text.Json.Serialization;

namespace SensorService.Application.DTOs.Esp32Status;

public class Esp32StatusPayloadDto
{
    [JsonPropertyName("esp32Id")]
    public string Esp32Id { get; set; } = default!;

    [JsonPropertyName("status")]
    public string Status { get; set; } = default!;

    [JsonPropertyName("timestamp")]
    public DateTime? Timestamp { get; set; }

    [JsonPropertyName("freeHeap")]
    public long? FreeHeap { get; set; }

    [JsonPropertyName("uptime")]
    public long? Uptime { get; set; }

    public bool IsOnline => string.Equals(Status, "online", StringComparison.OrdinalIgnoreCase);
    public bool IsOffline => string.Equals(Status, "offline", StringComparison.OrdinalIgnoreCase);
    public bool IsValidStatus => IsOnline || IsOffline;
}