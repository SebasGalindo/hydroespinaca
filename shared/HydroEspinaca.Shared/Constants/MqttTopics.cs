namespace HydroEspinaca.Shared.Constants;

public static class MqttTopics
{
    public static class Actuator
    {
        public const string RoutineCommands = "actuator/routine/commands";
        public const string RoutineCompletions = "actuator/routine/completions";
        public const string RoutineNotifications = "actuator/routine/notifications";
        public const string JobSchedule = "actuator/job/schedule";
    }

    public static class Sensor
    {
        public const string AllReadings = "sensor/#";
        public const string ReadingBatches = "sensor/readings";
    }
}