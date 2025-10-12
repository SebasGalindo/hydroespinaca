using System.Text.Json.Serialization;

namespace HydroEspinaca.Shared.DTOs.Mqtt;

public class ReadingBatchDto
{
    [JsonPropertyName("esp32Id")]
    public string Esp32Id { get; set; } = default!;

    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; set; }

    [JsonPropertyName("readings")]
    public List<ReadingInput> Readings { get; set; } = new();
}

public class ReadingInput
{
    [JsonPropertyName("physicalId")]
    public string PhysicalId { get; set; } = default!;

    [JsonPropertyName("variableCode")]
    public string VariableCode { get; set; } = default!;

    [JsonPropertyName("value")]
    public double Value { get; set; }
}