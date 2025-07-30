using System.Text.Json.Serialization;

namespace HydroEspinaca.Shared.Enums;
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TriggerType
{
    Manual,
    Fuzzy,
    Routine
}