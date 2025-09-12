using HydroEspinaca.Shared.Enums;

namespace HydroEspinaca.Shared.Constants;

public static class ActuatorConstants
{
    public static class Modes
    {
        public const string Digital = nameof(ActuatorMode.DIGITAL);
        public const string Pwm = nameof(ActuatorMode.PWM);
        
        public static readonly string[] ValidModes = { Digital, Pwm };
    }
    
    public static class PowerStates
    {
        public const string On = nameof(PowerState.ON);
        public const string Off = nameof(PowerState.OFF);
        
        public static readonly string[] ValidPowerStates = { On, Off };
    }

    public static class Channels
    {
        public const int MaxChannels = 2;
        public const int MinChannelId = 0;
        public const int MaxChannelId = MaxChannels - 1;
    }

    public static class Validation
    {
        public const int MinDutyCycle = 0;
        public const int MaxDutyCycle = 100;
        public const int MaxCodeLength = 100;
        public const int MaxLocationLength = 100;
        public const int MinDuration = 1; // Greater than 0 seconds
        public const int ObjectIdLength = 24; // MongoDB ObjectId hex string length
    }

    public static class Cleanup
    {
        public const int CleanupIntervalDays = 7;
        public const int RecordRetentionDays = 14;
        public const int ErrorRetryDelayHours = 1;
    }

    public static class CommandStatuses
    {
        public const string Scheduled = nameof(RoutineCommandStatus.SCHEDULED);
        public const string Running = nameof(RoutineCommandStatus.IN_PROGRESS);
        public const string Completed = nameof(RoutineCommandStatus.COMPLETED);
        public const string Failed = nameof(RoutineCommandStatus.FAILED);
        public const string Cancelled = nameof(RoutineCommandStatus.CANCELLED);
    }

    public static class StepStatuses
    {
        public const string Ok = "ok";
        public const string Cancelled = "cancelled";
        public const string Error = "error";
        
        public static readonly string[] ValidStatuses = { Ok, Cancelled, Error };
    }

    public static class FirmwareDecisions
    {
        public const string Consolidated = "consolidated";
        public const string Queued = "queued";
        
        public static readonly string[] ValidDecisions = { Consolidated, Queued };
    }
}