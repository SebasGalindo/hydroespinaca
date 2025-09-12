using HydroEspinaca.Shared.Enums;

namespace HydroEspinaca.Shared.Constants;

public static class SensorConstants
{
    public static class Cleanup
    {
        public const int CleanupIntervalDays = 7; // Weekly cleanup
        public const int AlertRetentionDays = 30; // Keep alerts for 30 days
        public const int ReadingRetentionDays = 90; // Keep readings for 90 days
        public const int ErrorRetryDelayHours = 1; // Retry after 1 hour on error
    }

    public static class Status
    {
        public const string Active = nameof(Esp32Status.Active);
        public const string Offline = nameof(Esp32Status.Offline);
        public const string Maintenance = nameof(Esp32Status.Maintenance);
        
        public static readonly string[] ValidStatuses = { Active, Offline, Maintenance };
    }
    
    public static class Validation
    {
        public const int MaxNameLength = 100;
        public const int MaxLocationLength = 200;
        public const int MaxAlertMessageLength = 500;
        public const int ObjectIdLength = 24; // MongoDB ObjectId hex string length
    }
}