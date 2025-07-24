using System.Text.Json.Serialization;

namespace HydroEspinaca.Shared.Enums;
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ActuatorType
{
    Led,
    Fan,
    Pump,
    Heater,
    AirPump,
    Unknown
}
