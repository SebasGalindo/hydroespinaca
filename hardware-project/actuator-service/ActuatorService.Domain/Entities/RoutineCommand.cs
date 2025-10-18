using HydroEspinaca.Shared.Abstractions;
using HydroEspinaca.Shared.Enums;
using System.Text.Json.Serialization;

namespace ActuatorService.Domain.Entities;

/// <summary>
/// Represents a single actuator activation command tracked in the database.
/// Commands with power: "OFF" are not stored.
/// Only one RUNNING command per actuator is allowed at a time.
/// </summary>
public class RoutineCommand : IIdentifiableMutable
{
    /// <summary>
    /// Auto-generated MongoDB identifier
    /// </summary>
    public string Id { get; private set; } = default!;

    /// <summary>
    /// Unique command identifier: {actuatorCode}_{timestamp}
    /// </summary>
    public string CommandId { get; set; } = default!;

    /// <summary>
    /// Actuator code (e.g., "calefactor-agua", "bomba-recirculacion")
    /// </summary>
    public string ActuatorCode { get; set; } = default!;

    /// <summary>
    /// ESP32 device identifier
    /// </summary>
    public string Esp32Id { get; set; } = default!;

    /// <summary>
    /// Current status: RUNNING or FINISHED
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public RoutineCommandStatus StatusGeneral { get; set; } = RoutineCommandStatus.RUNNING;

    /// <summary>
    /// When the command was initially created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the command was last extended (optional)
    /// Updated when a new activation request arrives for an already RUNNING actuator
    /// </summary>
    public DateTime? ExtendedAt { get; set; }

    /// <summary>
    /// When the firmware confirmed completion via MQTT
    /// </summary>
    public DateTime? FinishedAt { get; set; }

    /// <summary>
    /// Total duration in seconds from creation to completion.
    /// Calculated as: (FinishedAt - CreatedAt).TotalSeconds
    /// Only set when status is FINISHED, null while RUNNING.
    /// </summary>
    public double? TotalDurationSeconds { get; set; }

    public void SetId(string id) => Id = id;
}