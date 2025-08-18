namespace AuthService.Application.Exceptions;
public class ClientAppAlreadyExistsException : Exception
{
    public ClientAppAlreadyExistsException(string clientId)
        : base($"Ya existe una aplicación cliente con clientId '{clientId}'.") { }
}