using Microsoft.AspNetCore.Mvc;

namespace HydroEspinaca.Shared.Authentication.Interfaces;

/// <summary>
/// Interface for mapping exceptions to ProblemDetails responses
/// </summary>
public interface IExceptionToProblemDetailsMapper
{
    /// <summary>
    /// Maps an exception to a ProblemDetails object
    /// </summary>
    /// <param name="exception">The exception to map</param>
    /// <param name="requestPath">The request path where the exception occurred</param>
    /// <param name="isDevelopment">Whether the application is running in development mode</param>
    /// <returns>A ProblemDetails object representing the exception</returns>
    ProblemDetails MapToProblemDetails(Exception exception, string requestPath, bool isDevelopment);
    
    /// <summary>
    /// Checks if the mapper can handle the given exception type
    /// </summary>
    /// <param name="exception">The exception to check</param>
    /// <returns>True if the mapper can handle the exception, false otherwise</returns>
    bool CanHandle(Exception exception);
    
    /// <summary>
    /// Gets the HTTP status code for the given exception
    /// </summary>
    /// <param name="exception">The exception</param>
    /// <returns>The appropriate HTTP status code</returns>
    int GetStatusCode(Exception exception);
}