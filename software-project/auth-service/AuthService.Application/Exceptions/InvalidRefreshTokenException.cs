namespace AuthService.Application.Exceptions;

public class InvalidRefreshTokenException : Exception
{
    public InvalidRefreshTokenException()
        : base("Refresh token is invalid or expired.") { }
}
