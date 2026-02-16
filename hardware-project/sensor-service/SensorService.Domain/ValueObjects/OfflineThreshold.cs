namespace SensorService.Domain.ValueObjects;

/// <summary>
/// Objeto de valor que define el umbral de tiempo para considerar un nodo ESP32 como desconectado.
/// Encapsula la lógica de determinación de estado offline basada en la última actividad registrada.
/// </summary>
public record OfflineThreshold
{
    /// <summary>
    /// Duración del umbral de desconexión.
    /// </summary>
    public TimeSpan Duration { get; }

    private OfflineThreshold(TimeSpan duration)
    {
        Duration = duration;
    }

    /// <summary>
    /// Crea una nueva instancia de umbral de desconexión con la duración especificada.
    /// </summary>
    /// <param name="duration">Duración del umbral. Debe ser positiva.</param>
    /// <returns>Una nueva instancia de <see cref="OfflineThreshold"/>.</returns>
    /// <exception cref="ArgumentException">Si la duración es menor o igual a cero.</exception>
    public static OfflineThreshold Create(TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero)
            throw new ArgumentException("Offline threshold must be positive", nameof(duration));

        return new OfflineThreshold(duration);
    }

    /// <summary>
    /// Crea un umbral de desconexión a partir de un valor en minutos.
    /// </summary>
    /// <param name="minutes">Cantidad de minutos para el umbral.</param>
    /// <returns>Una nueva instancia de <see cref="OfflineThreshold"/>.</returns>
    public static OfflineThreshold FromMinutes(int minutes) => Create(TimeSpan.FromMinutes(minutes));

    /// <summary>
    /// Determina si un nodo está offline según su última actividad y el umbral configurado.
    /// </summary>
    /// <param name="lastActivity">Fecha y hora de la última actividad registrada.</param>
    /// <param name="currentTime">Fecha y hora actual para la comparación.</param>
    /// <returns><c>true</c> si el tiempo transcurrido supera el umbral; de lo contrario, <c>false</c>.</returns>
    public bool IsOffline(DateTime lastActivity, DateTime currentTime)
    {
        return (currentTime - lastActivity) > Duration;
    }

    public static implicit operator TimeSpan(OfflineThreshold threshold) => threshold.Duration;
}