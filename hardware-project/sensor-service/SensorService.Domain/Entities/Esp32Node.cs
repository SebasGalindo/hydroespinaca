using HydroEspinaca.Shared.Abstractions;
using HydroEspinaca.Shared.Enums;

namespace SensorService.Domain.Entities;

/// <summary>
/// Representa un nodo microcontrolador ESP32 en el sistema IoT hidropónico.
/// Cada nodo puede tener múltiples sensores conectados y reporta telemetría periódicamente.
/// </summary>
public class Esp32Node : IIdentifiableMutable
{
    /// <summary>
    /// Identificador único del nodo ESP32 asignado por la base de datos.
    /// </summary>
    public string Id { get; private set; } = default!;

    /// <summary>
    /// Nombre amigable del nodo ESP32 (obligatorio).
    /// </summary>
    public string Name { get; set; } = default!;

    /// <summary>
    /// Ubicación física del nodo ESP32 dentro del sistema hidropónico (obligatorio).
    /// </summary>
    public string Location { get; set; } = default!;

    /// <summary>
    /// Fecha y hora de la última vez que el nodo reportó datos al sistema.
    /// </summary>
    public DateTime LastSeen { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Estado actual del nodo ESP32 (Activo u Offline).
    /// </summary>
    public Esp32Status Status { get; set; } = HydroEspinaca.Shared.Enums.Esp32Status.Active;

    /// <summary>
    /// Tiempo de actividad del nodo en segundos desde su último reinicio.
    /// </summary>
    public long Uptime { get; set; } = 0;

    /// <summary>
    /// Memoria heap libre disponible en el nodo, expresada en bytes.
    /// </summary>
    public long FreeHeap { get; set; } = 0;

    /// <summary>
    /// Establece el identificador único del nodo ESP32.
    /// </summary>
    /// <param name="id">Identificador único asignado por la base de datos.</param>
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
