using HydroEspinaca.Shared.Abstractions;
using HydroEspinaca.Shared.Enums;

namespace SensorService.Domain.Entities;
public class SensorAlert : IIdentifiableMutable
{
    public string Id { get; private set; } = default!;
    public string SensorId { get; set; } = default!;
    public AlertType Type { get; set; } = default!; // threshold-exceeded, disconnected, etc.
    public double Value { get; set; }
    public double Threshold { get; set; }
    public DateTime Timestamp { get; set; }
    public string Message { get; set; } = default!;
    public AlertSeverity Severity { get; set; }
    public bool Acknowledged { get; set; } = false;

    public void SetId(string id) => Id = id;
}
