using System.Text.Json.Serialization;

namespace HydroEspinaca.Shared.Enums;
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ActuatorStatus
{
    Active,
    Inactive,
    Maintenance
}
