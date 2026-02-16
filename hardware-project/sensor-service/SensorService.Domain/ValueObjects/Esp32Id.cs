namespace SensorService.Domain.ValueObjects;

/// <summary>
/// Objeto de valor que encapsula el identificador único de un nodo ESP32.
/// Garantiza que el valor no sea nulo ni vacío y realiza normalización (trim).
/// </summary>
public record Esp32Id
{
    /// <summary>
    /// Valor del identificador único del nodo ESP32.
    /// </summary>
    public string Value { get; }

    private Esp32Id(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Crea una nueva instancia de identificador ESP32 con validación.
    /// </summary>
    /// <param name="value">Valor del identificador. No puede ser nulo ni vacío.</param>
    /// <returns>Una nueva instancia de <see cref="Esp32Id"/>.</returns>
    /// <exception cref="ArgumentException">Si el valor es nulo o vacío.</exception>
    public static Esp32Id Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("ESP32 ID cannot be null or empty", nameof(value));

        return new Esp32Id(value.Trim());
    }

    public static implicit operator string(Esp32Id esp32Id) => esp32Id.Value;
    public static implicit operator Esp32Id(string value) => Create(value);

    public override string ToString() => Value;
}