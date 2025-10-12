using HydroEspinaca.Shared.Abstractions;
using System;

namespace ActuatorService.Domain.Entities;

public class ControlOutput : IIdentifiableMutable
{
    public string Id { get; private set; } = default!;

    /// <summary>
    /// Unique code for this control output (e.g., OUTPUT_VENTILADOR_POTENCIA)
    /// </summary>
    public string Code { get; set; } = default!;

    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string? Unit { get; set; }

    /// <summary>
    /// Code of the actuator this output controls (e.g., Ventiladores)
    /// Used for stable cross-service references
    /// </summary>
    public string ActuatorCode { get; set; } = default!;

    /// <summary>
    /// ID of the actuator this output controls (MongoDB ObjectId)
    /// Used for internal service queries and joins
    /// </summary>
    public string? ActuatorId { get; set; }

    public double MinValue { get; set; }
    public double MaxValue { get; set; }
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    public void SetId(string id) => Id = id;
}
