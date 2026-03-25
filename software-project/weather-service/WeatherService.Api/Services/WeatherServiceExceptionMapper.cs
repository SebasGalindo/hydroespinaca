using FluentValidation;
using HydroEspinaca.Shared.Authentication.Interfaces;
using HydroEspinaca.Shared.Errors;
using Microsoft.AspNetCore.Mvc;

namespace WeatherService.Api.Services;

/// <summary>
/// Weather service specific exception mapper that extends the shared mapper.
/// </summary>
public class WeatherServiceExceptionMapper : IExceptionToProblemDetailsMapper
{
    private readonly ProblemDetailsFactory _sharedMapper;

    public WeatherServiceExceptionMapper(ProblemDetailsFactory sharedMapper)
    {
        _sharedMapper = sharedMapper;
    }

    /// <summary>
    /// Maps exceptions to ProblemDetails responses. 
    /// </summary>
    /// <param name="exception">The exception to map.</param>
    /// <param name="requestPath">The request path associated with the exception.</param>
    /// <param name="isDevelopment">Whether the application is running in development mode.</param>
    /// <returns>A ProblemDetails object representing the mapped exception.</returns>
    public ProblemDetails MapToProblemDetails(Exception exception, string requestPath, bool isDevelopment)
    {
        return exception switch
        {
            ValidationException ex => CreateValidationProblemDetails(ex, requestPath),

            KeyNotFoundException ex => CreateProblemDetails(
                "Resource Not Found", ex.Message, 404,
                "https://tools.ietf.org/html/rfc9110#section-15.5.5",
                requestPath, isDevelopment),

            ArgumentException ex => CreateProblemDetails(
                "Invalid Request", ex.Message, 400,
                "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                requestPath, isDevelopment),

            InvalidOperationException ex => CreateProblemDetails(
                "Service Unavailable", ex.Message, 503,
                "https://tools.ietf.org/html/rfc9110#section-15.6.4",
                requestPath, isDevelopment),

            _ => _sharedMapper.MapToProblemDetails(exception, requestPath, isDevelopment)
        };
    }

    /// <summary>
    /// Determines whether this mapper can handle the given exception type. 
    /// </summary>
    /// <param name="exception">The exception to check.</param>
    /// <returns>True if this mapper can handle the exception, otherwise false.</returns>
    public bool CanHandle(Exception exception)
    {
        return exception is KeyNotFoundException or ArgumentException or InvalidOperationException
            || _sharedMapper.CanHandle(exception);
    }

    /// <summary>
    /// Gets the appropriate HTTP status code for the given exception type.
    /// </summary>
    /// <param name="exception">The exception for which to get the status code.</param>
    /// <returns>The HTTP status code.</returns>
    public int GetStatusCode(Exception exception)
    {
        return exception switch
        {
            ValidationException => 400,
            KeyNotFoundException => 404,
            ArgumentException => 400,
            InvalidOperationException => 503,
            _ => _sharedMapper.GetStatusCode(exception)
        };
    }

    /// <summary>
    /// Creates a ValidationProblemDetails object from a FluentValidation ValidationException, 
    /// grouping errors by property name and formatting them according to RFC 7807. 
    /// This provides a structured response for validation errors that can be easily consumed by clients.
    /// </summary>
    /// <param name="exception">The ValidationException to convert.</param>
    /// <param name="requestPath">The request path associated with the exception.</param>
    /// <returns>A ValidationProblemDetails object representing the validation errors.</returns>
    private static ValidationProblemDetails CreateValidationProblemDetails(ValidationException exception, string requestPath)
    {
        var errors = exception.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray());

        return new ValidationProblemDetails(errors)
        {
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
            Title = "Validation Error",
            Status = 400,
            Detail = "One or more validation errors occurred.",
            Instance = requestPath
        };
    }

    /// <summary>
    /// Creates a ProblemDetails object with the specified parameters, 
    /// formatting the detail message based on whether the application is in development mode.
    /// </summary>
    /// <param name="title">The title of the problem details.</param>
    /// <param name="detail">The detail message of the problem details.</param>
    /// <param name="status">The HTTP status code of the problem details.</param>
    /// <param name="type">The type URI of the problem details.</param>
    /// <param name="requestPath">The request path associated with the problem details.</param>
    /// <param name="isDevelopment">Whether the application is in development mode.</param>
    /// <returns>A ProblemDetails object.</returns>
    private static ProblemDetails CreateProblemDetails(
        string title, string detail, int status, string type, string requestPath, bool isDevelopment)
    {
        return new ProblemDetails
        {
            Type = type,
            Title = title,
            Status = status,
            Detail = isDevelopment ? detail : $"{title}. Please try again later.",
            Instance = requestPath
        };
    }
}
