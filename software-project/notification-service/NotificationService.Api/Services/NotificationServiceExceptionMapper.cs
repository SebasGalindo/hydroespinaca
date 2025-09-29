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

    public ProblemDetails MapToProblemDetails(Exception exception, string requestPath, bool isDevelopment)
    {
        // Handle notification-service specific exceptions with custom titles/messages
        return exception switch
        {
            ValidationException ex => CreateValidationProblemDetails(ex, requestPath),
            
            // Add notification-specific exceptions here as they are created
            // EmailDeliveryException ex => CreateProblemDetails(
            //     "Email Delivery Failed", ex.Message, 502, 
            //     "https://tools.ietf.org/html/rfc9110#section-15.6.3", 
            //     requestPath, isDevelopment),
            
            // Delegate to shared mapper for all other exceptions
            _ => _sharedMapper.MapToProblemDetails(exception, requestPath, isDevelopment)
        };
    }

    public bool CanHandle(Exception exception)
    {
        // Let the shared mapper handle most exceptions through inheritance
        return _sharedMapper.CanHandle(exception);
    }

    public int GetStatusCode(Exception exception)
    {
        return exception switch
        {
            ValidationException => 400,
            // Add notification-specific exceptions here
            // EmailDeliveryException => 502,
            _ => _sharedMapper.GetStatusCode(exception)
        };
    }

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