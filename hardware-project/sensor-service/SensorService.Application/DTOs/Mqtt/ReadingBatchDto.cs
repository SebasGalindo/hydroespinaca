using System.Text.Json.Serialization;

namespace SensorService.Application.DTOs.Mqtt;
public class ReadingBatchDto
{
    [JsonPropertyName("esp32Id")]
    public string Esp32Id { get; set; } = default!;

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    [JsonPropertyName("readings")]
    public List<ReadingInput> Readings { get; set; } = new();
}

public class ReadingInput
{
    [JsonPropertyName("physicalId")]
    public string PhysicalId { get; set; } = default!;

    [JsonPropertyName("variableId")]
    public string VariableId { get; set; } = default!;

    [JsonPropertyName("value")]
    public double Value { get; set; }
}