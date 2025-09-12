namespace HydroEspinaca.Shared.Errors;

/// <summary>
/// Base exception for 404 Not Found errors
/// </summary>
public abstract class NotFoundException : DomainException
{
    protected NotFoundException(string message) : base(message) { }
}

/// <summary>
/// Base exception for 409 Conflict errors
/// </summary>
public abstract class ConflictException : DomainException
{
    protected ConflictException(string message) : base(message) { }
}

/// <summary>
/// Base exception for 422 Unprocessable Entity errors
/// </summary>
public abstract class UnprocessableEntityException : DomainException
{
    protected UnprocessableEntityException(string message) : base(message) { }
}

/// <summary>
/// Base exception for 502 Bad Gateway errors (service communication issues)
/// </summary>
public abstract class ServiceUnavailableException : DomainException
{
    protected ServiceUnavailableException(string message) : base(message) { }
    protected ServiceUnavailableException(string message, Exception innerException) : base(message, innerException) { }
}