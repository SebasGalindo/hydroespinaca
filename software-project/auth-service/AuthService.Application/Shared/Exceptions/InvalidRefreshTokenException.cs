namespace AuthService.Application.Exceptions;

/// <summary>
/// Exception thrown when a refresh token is invalid, expired, or has been revoked.
/// </summary>
public class InvalidRefreshTokenException : Exception
{
    public InvalidRefreshTokenException()
        : base("Refresh token is invalid or expired.") { }
}
