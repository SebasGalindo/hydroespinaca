namespace HydroEspinaca.Shared.DTOs.Actuator;

public class RoutineNotificationDto
{
    public string Esp32Id { get; set; } = default!;
    public DateTime Timestamp { get; set; }
    public string Decision { get; set; } = default!;  // "consolidated" | "queued"
    public int ChannelId { get; set; }
    public string AffectedCommand { get; set; } = default!;
    public string TargetCommand { get; set; } = default!;
    public string ExecutionLog { get; set; } = default!;
}