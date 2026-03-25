namespace AuthService.Application.Exceptions;

/// <summary>
/// Exception thrown when client credentials (client_id/client_secret) are invalid.
/// </summary>
public class InvalidClientCredentialsException : Exception
{
    public InvalidClientCredentialsException()
        : base("Client credentials inválidas.") { }
}