using HydroEspinaca.Shared.Errors;

namespace BffService.Domain.Exceptions;

/// <summary>
/// Exception thrown when a user session cannot be found in the session store.
/// </summary>
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

public class ServiceException : DomainException
{
    public int StatusCode { get; }

    public ServiceException(string message, int statusCode) : base(message)
    {
        StatusCode = statusCode;
    }

    public ServiceException(string message, int statusCode, Exception innerException) : base(message, innerException)
    {
        StatusCode = statusCode;
    }
}