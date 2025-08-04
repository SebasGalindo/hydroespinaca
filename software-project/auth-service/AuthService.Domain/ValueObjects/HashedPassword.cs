namespace AuthService.Domain.ValueObjects;
public sealed class HashedPassword
{
    public string Value { get; }
    public HashedPassword(string hashedValue)
    {
        if (string.IsNullOrWhiteSpace(hashedValue)) throw new ArgumentException("Password hasheado inválido.");
        Value = hashedValue;
    }

    public override bool Equals(object? obj) => obj is HashedPassword other && Value == other.Value;
    public override int GetHashCode() => Value.GetHashCode();
}