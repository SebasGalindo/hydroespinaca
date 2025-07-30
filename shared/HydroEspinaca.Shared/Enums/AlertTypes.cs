using System.Text.Json.Serialization;

namespace HydroEspinaca.Shared.Enums;
[JsonConverter(typeof(JsonStringEnumConverter))]

public enum AlertType
{
    OutOfRange,
    Anomaly,
    InactiveSensor,
    Esp32Offline
}
