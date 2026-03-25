namespace AuthService.Application.Exceptions;

/// <summary>
/// Exception thrown when attempting to register a client application with a client_id that already exists.
/// </summary>
public class ClientAppAlreadyExistsException : Exception
{
    public ClientAppAlreadyExistsException(string clientId)
        : base($"Ya existe una aplicación cliente con clientId '{clientId}'.") { }
}