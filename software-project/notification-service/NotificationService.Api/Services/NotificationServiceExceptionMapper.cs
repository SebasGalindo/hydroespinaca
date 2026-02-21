using FluentValidation;
using HydroEspinaca.Shared.Authentication.Interfaces;
using HydroEspinaca.Shared.Errors;
using Microsoft.AspNetCore.Mvc;

namespace NotificationService.Api.Services;

/// <summary>
/// Notification service specific exception mapper that extends the shared mapper
/// </summary>
public class NotificationServiceExceptionMapper : IExceptionToProblemDetailsMapper
{
    private readonly ProblemDetailsFactory _sharedMapper;

    public NotificationServiceExceptionMapper(ProblemDetailsFactory sharedMapper)
    {
        _sharedMapper = sharedMapper;
    }

    /// Maps exceptions to ProblemDetails, handling notification-service specific exceptions with custom titles and messages, while delegating all other exceptions to the shared mapper for consistent error responses across microservices.       
    public ProblemDetails MapToProblemDetails(Exception exception, string requestPath, bool isDevelopment)
    {
        // Handle notification-service specific exceptions with custom titles/messages
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

            // Delegate to shared mapper for all other exceptions
            _ => _sharedMapper.MapToProblemDetails(exception, requestPath, isDevelopment)
        };
    }

    ///  <summary>
    /// Determines if this mapper can handle a specific exception.
    /// </summary>
    /// <param name="exception">The exception to check.</param>
    /// <returns>True if the exception is a KeyNotFoundException or ArgumentException, or if the shared mapper can handle it; otherwise, false.</returns>
    public bool CanHandle(Exception exception)
    {
        return exception is KeyNotFoundException or ArgumentException
            || _sharedMapper.CanHandle(exception);
    }

    /// <summary> 
    /// Gets the appropriate HTTP status code for a given exception, 
    /// returning specific codes for notification-service exceptions and delegating to 
    /// the shared mapper for others. 
    /// </summary>
    /// <param name="exception">The exception for which to get the status code.</param>
    /// <returns>The HTTP status code corresponding to the exception.</returns>
    public int GetStatusCode(Exception exception)
    {
        return exception switch
        {
            ValidationException => 400,
            KeyNotFoundException => 404,
            ArgumentException => 400,
            _ => _sharedMapper.GetStatusCode(exception)
        };
    }

    /// <summary>
    /// Creates a ValidationProblemDetails object from a FluentValidation ValidationException,
    /// grouping errors by property name and providing a clear structure for validation errors in the response.
    /// </summary> 
    /// <param name="exception">The ValidationException containing the validation errors.</param>
    /// <param name="requestPath">The path of the request that caused the exception, used for the Instance property of the ProblemDetails.</param>
    /// <returns>A ValidationProblemDetails object containing the validation errors and appropriate metadata.</returns>
    private static ValidationProblemDetails CreateValidationProblemDetails(ValidationException exception, string requestPath)
    {
        var errors = new Dictionary<string, string[]>();

        if (exception.Errors != null && exception.Errors.Any())
        {
            errors = exception.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => string.IsNullOrEmpty(g.Key) ? "General" : g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray()
                );
        }

        return new ValidationProblemDetails(errors)
        {
            Title = "One or more validation errors occurred.",
            Status = 400,
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
            Instance = requestPath
        };
    }

    /// <summary>
    /// Creates a ProblemDetails object with a given title, detail message, status code, type URI,
    /// and request path. The detail message is included in development environments 
    /// for debugging purposes, while in production environments a generic message based on the status code is returned to avoid exposing sensitive information.
    /// </summary>
    /// <param name="title">The title of the problem, providing a short summary of the error.</param>
    /// <param name="detail">The detailed error message, which may contain sensitive information and should only be included in development environments.</param>
    /// <param name="statusCode">The HTTP status code that corresponds to the error.</param>
    /// <param name="type">A URI reference that identifies the problem type, ideally linking to documentation about the error.</param>
    /// <param name="requestPath">The path of the request that caused the error, used for the Instance property of the ProblemDetails.</param>
    /// <param name="isDevelopment">A boolean indicating whether the application is running in a development environment, which determines whether to include the detailed error message or a generic message.</param>
    /// <returns>A ProblemDetails object containing the error information and appropriate metadata for the response.</returns>
    private static ProblemDetails CreateProblemDetails(
        string title, string detail, int statusCode, string type, 
        string requestPath, bool isDevelopment)
    {
        return new ProblemDetails
        {
            Title = title,
            Detail = isDevelopment ? detail : GetSafeMessage(statusCode),
            Status = statusCode,
            Type = type,
            Instance = requestPath
        };
    }

    /// <summary>
    /// Provides a generic message based on the HTTP status code for production environments,
    /// to avoid exposing sensitive information while still giving a meaningful response to the client.
    /// </summary>
    /// <param name="statusCode">The HTTP status code for which to get the generic message.</param>
    /// <returns>A generic message corresponding to the status code.</returns>
    private static string GetSafeMessage(int statusCode)
    {
        return statusCode switch
        {
            400 => "Invalid request",
            401 => "Authentication required",
            403 => "Forbidden",
            404 => "Resource not found",
            502 => "External service unavailable",
            _ => "An error occurred"
        };
    }
}