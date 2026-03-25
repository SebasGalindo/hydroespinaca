namespace SensorService.Domain.ValueObjects;

/// <summary>
/// Objeto de valor que representa el estado actual de un nodo ESP32,
/// incluyendo si está offline y el tiempo transcurrido desde su última actividad.
/// </summary>
public record Esp32StatusRecord
{
    /// <summary>
    /// Identificador único del nodo ESP32.
    /// </summary>
    public Esp32Id Esp32Id { get; }

    /// <summary>
    /// Fecha y hora de la última actividad registrada del nodo.
    /// </summary>
    public DateTime LastActivity { get; }

    /// <summary>
    /// Indica si el nodo ESP32 está offline según el umbral configurado.
    /// </summary>
    public bool IsOffline { get; }

    /// <summary>
    /// Tiempo transcurrido desde la última actividad del nodo.
    /// </summary>
    public TimeSpan TimeSinceLastActivity { get; }

    private Esp32StatusRecord(Esp32Id esp32Id, DateTime lastActivity, bool isOffline, TimeSpan timeSinceLastActivity)
    {
        Esp32Id = esp32Id;
        LastActivity = lastActivity;
        IsOffline = isOffline;
        TimeSinceLastActivity = timeSinceLastActivity;
    }

    /// <summary>
    /// Crea una instancia de estado ESP32 evaluando si el nodo está offline según el umbral configurado.
    /// </summary>
    /// <param name="esp32Id">Identificador del nodo ESP32.</param>
    /// <param name="lastActivity">Última actividad registrada del nodo.</param>
    /// <param name="currentTime">Fecha y hora actual.</param>
    /// <param name="threshold">Umbral de desconexión configurado.</param>
    /// <returns>Un registro con el estado calculado del nodo ESP32.</returns>
    public static Esp32StatusRecord Create(
        Esp32Id esp32Id,
        DateTime lastActivity,
        DateTime currentTime,
        OfflineThreshold threshold)
    {
        var timeSinceLastActivity = currentTime - lastActivity;
        var isOffline = threshold.IsOffline(lastActivity, currentTime);

        return new Esp32StatusRecord(esp32Id, lastActivity, isOffline, timeSinceLastActivity);
    }
}