namespace SensorService.Domain.Constants;

public static class AlertMessages
{
    public static class Esp32Offline
    {
        public const string Disconnected = "Se ha desconectado inesperadamente.";
        public const string Reconnected = "Se ha reconectado exitosamente.";
    }
}