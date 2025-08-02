using System.Text.RegularExpressions;

namespace AuthService.Domain.ValueObjects;
public sealed class Email
{
    public  string Value { get; set; }

    public Email(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Email no puede estar vacío.");
        if (!IsValidEmail(value)) throw new ArgumentException("Email no tiene un formato válido.", nameof(value));

        Value = value;
    }


    private static bool IsValidEmail(string email)
    {
        string emailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
        return Regex.IsMatch(email, emailPattern);
    }

    public override bool Equals(object? obj) => obj is Email other && Value == other.Value;
    public override int GetHashCode() => Value.GetHashCode();
}