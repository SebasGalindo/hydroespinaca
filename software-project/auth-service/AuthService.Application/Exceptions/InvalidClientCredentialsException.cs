namespace AuthService.Application.Exceptions;
public class InvalidClientCredentialsException : Exception
{
    public InvalidClientCredentialsException()
        : base("Client credentials inválidas.") { }
}