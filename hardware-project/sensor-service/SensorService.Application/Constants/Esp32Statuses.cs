namespace SensorService.Application.Constants;

public static class Esp32Statuses
{
    public const string Active = "active";
    public const string Offline = "offline";
    public const string Maintenance = "maintenance";

    public static readonly string[] All = { Active, Offline, Maintenance };
}
