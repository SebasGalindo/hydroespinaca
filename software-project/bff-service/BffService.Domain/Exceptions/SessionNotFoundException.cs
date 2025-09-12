using HydroEspinaca.Shared.Errors;

namespace BffService.Domain.Exceptions;

public class SessionNotFoundException : NotFoundException
{
    public SessionNotFoundException(string sessionId)
        : base($"Session with ID '{sessionId}' was not found.")
    {
    }
}

public class SessionExpiredException : DomainException
{
    public SessionExpiredException(string sessionId)
        : base($"Session with ID '{sessionId}' has expired.")
    {
    }
}

public class InvalidTokenException : DomainException
{
    public InvalidTokenException(string message) : base(message)
    {
    }
}

public class ProxyException : ServiceUnavailableException
{
    public ProxyException(string message) : base(message)
    {
    }

    public ProxyException(string message, Exception innerException) : base(message, innerException)
    {
    }
}