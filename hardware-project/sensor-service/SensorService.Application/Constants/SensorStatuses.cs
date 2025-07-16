namespace SensorService.Application.Constants;

public static class SensorStatuses
{
    public const string Active = "Active";
    public const string Inactive = "Inactive";
    public const string Disconnected = "Disconnected";

    public static readonly string[] All = { Active, Inactive, Disconnected };
}
