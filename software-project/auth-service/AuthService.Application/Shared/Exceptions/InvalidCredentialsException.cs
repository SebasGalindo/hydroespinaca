namespace AuthService.Application.Exceptions;

/// <summary>
/// Exception thrown when user login credentials (email/password) are invalid.
/// </summary>
public class InvalidCredentialsException : Exception
{
    public InvalidCredentialsException()
        : base("Invalid email or password.") { }
}
