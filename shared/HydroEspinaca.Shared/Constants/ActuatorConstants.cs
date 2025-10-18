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
        public const string Running = nameof(RoutineCommandStatus.RUNNING);
        public const string Finished = nameof(RoutineCommandStatus.FINISHED);

        public static readonly string[] ValidStatuses = { Running, Finished };
    }
}