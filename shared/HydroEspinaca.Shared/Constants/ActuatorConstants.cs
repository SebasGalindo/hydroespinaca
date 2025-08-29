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
}