using HydroEspinaca.Shared.Abstractions;
using HydroEspinaca.Shared.Enums;

namespace SensorService.Domain.Entities;
public abstract class AlertBase : IIdentifiableMutable
{
    public string Id { get; private set; } = default!;
    public DateTime Timestamp { get; set; }
    public string Message { get; set; } = default!;
    public bool Acknowledged { get; set; } = false;
    public DateTime? ResolvedAt { get; set; }

    // Legacy fields maintained for Esp32Alert compatibility (not used in SensorAlert)
    public AlertType? Type { get; set; }
    public AlertSeverity? Severity { get; set; }

    public void SetId(string id) => Id = id;
}
