namespace SensorService.Domain.Constants;

/// <summary>
/// Constantes con mensajes predefinidos para las alertas del sistema.
/// </summary>
public static class AlertMessages
{
    /// <summary>
    /// Mensajes relacionados con la desconexión y reconexión de nodos ESP32.
    /// </summary>
    public static class Esp32Offline
    {
        /// <summary>
        /// Mensaje predefinido cuando un nodo ESP32 se desconecta inesperadamente del sistema.
        /// </summary>
        public const string Disconnected = "Se ha desconectado inesperadamente.";

        /// <summary>
        /// Mensaje predefinido cuando un nodo ESP32 se reconecta exitosamente al sistema.
        /// </summary>
        public const string Reconnected = "Se ha reconectado exitosamente.";
    }
}