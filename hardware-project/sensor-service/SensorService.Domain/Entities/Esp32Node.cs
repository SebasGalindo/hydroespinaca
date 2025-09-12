using HydroEspinaca.Shared.Abstractions;
using HydroEspinaca.Shared.Enums;

namespace SensorService.Domain.Entities;

public class Esp32Node : IIdentifiableMutable
{
    public string Id { get; private set; } = default!;
    public string Name { get; set; } = default!; // Nombre amigable (obligatorio)
    public string Location { get; set; } = default!; // Ubicación física (obligatorio) 
    public DateTime LastSeen { get; set; } = DateTime.UtcNow; // Última vez que reportó algo
    public Esp32Status Status { get; set; } = HydroEspinaca.Shared.Enums.Esp32Status.Active;
    
    // Telemetría obligatoria - siempre debe tener valores
    public long Uptime { get; set; } = 0; // Segundos desde último reset
    public long FreeHeap { get; set; } = 0; // Bytes libres en heap

    public void SetId(string id) => Id = id;
    
    /// <summary>
    /// Actualiza el estado a online con nueva telemetría
    /// </summary>
    public void SetOnline(DateTime timestamp, long uptime, long freeHeap)
    {
        LastSeen = timestamp;
        Status = HydroEspinaca.Shared.Enums.Esp32Status.Active;
        Uptime = uptime;
        FreeHeap = freeHeap;
    }
    
    /// <summary>
    /// Actualiza el estado a offline preservando última telemetría
    /// </summary>
    public void SetOffline(DateTime timestamp)
    {
        LastSeen = timestamp;
        Status = HydroEspinaca.Shared.Enums.Esp32Status.Offline;
        // Preservamos uptime y freeHeap del último estado conocido
    }
}
