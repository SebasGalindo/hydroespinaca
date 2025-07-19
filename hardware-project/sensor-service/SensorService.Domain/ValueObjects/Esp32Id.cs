namespace SensorService.Domain.ValueObjects;

public record Esp32Id
{
    public string Value { get; }

    private Esp32Id(string value)
    {
        Value = value;
    }

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