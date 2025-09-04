using HydroEspinaca.Shared.Abstractions;
using HydroEspinaca.Shared.Enums;

namespace SensorService.Domain.Entities;

public class Esp32Node : IIdentifiableMutable
{
    public string Id { get; private set; } = default!;
    public string Name { get; set; } = default!; // Optional display name
    public string Location { get; set; } = default!;
    public DateTime LastSeen { get; set; } = DateTime.UtcNow;
    public Esp32Status Status { get; set; }
    
    // Telemetría básica
    public long? FreeHeap { get; set; }
    public long? Uptime { get; set; }
    public DateTime? LastHeartbeat { get; set; }

    public void SetId(string id) => Id = id;
    
    public void UpdateTelemetry(DateTime timestamp, long? freeHeap = null, long? uptime = null)
    {
        LastSeen = timestamp;
        LastHeartbeat = timestamp;
        if (freeHeap.HasValue) FreeHeap = freeHeap.Value;
        if (uptime.HasValue) Uptime = uptime.Value;
        Status = HydroEspinaca.Shared.Enums.Esp32Status.Active;
    }
}
