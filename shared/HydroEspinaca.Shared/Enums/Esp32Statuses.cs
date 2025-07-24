using System.Text.Json.Serialization;

namespace HydroEspinaca.Shared.Enums;
[JsonConverter(typeof(JsonStringEnumConverter))]

public enum Esp32Status
{
    Active,
    Offline,
    Maintenance
}
