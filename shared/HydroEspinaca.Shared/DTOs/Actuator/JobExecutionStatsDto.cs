namespace HydroEspinaca.Shared.DTOs.Actuator;

/// <summary>
/// Statistics about job execution across all ESP32 devices
/// </summary>
public class JobExecutionStatsDto
{
    public int ActiveCount { get; set; }
    public int PendingCount { get; set; }
    public int TotalLockedPins { get; set; }
    public List<string> Esp32Ids { get; set; } = new();
}
