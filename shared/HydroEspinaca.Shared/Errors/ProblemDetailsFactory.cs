using FluentValidation;
using HydroEspinaca.Shared.Authentication.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HydroEspinaca.Shared.Errors;

/// <summary>
/// Factory for creating ProblemDetails from exceptions
/// </summary>
public class ProblemDetailsFactory : IExceptionToProblemDetailsMapper
{
    public ProblemDetails MapToProblemDetails(Exception exception, string requestPath, bool isDevelopment)
    {
        // Handle ValidationException specially for detailed error information
        if (exception is ValidationException validationException)
        {
            return CreateValidationProblemDetails(validationException, requestPath);
        }

        var exceptionType = exception.GetType();
        var statusCode = ExceptionMappings.GetStatusCode(exceptionType);
        var title = ExceptionMappings.GetTitle(exceptionType);
        var problemType = ExceptionMappings.GetProblemType(exceptionType);

        var problemDetails = new ProblemDetails
        {
            Title = title,
            Detail = isDevelopment ? exception.Message : GetSafeMessage(exception, statusCode),
            Status = statusCode,
            Type = problemType,
            Instance = requestPath
        };

        // Add stack trace in development mode for server errors
        if (isDevelopment && statusCode >= 500)
        {
            problemDetails.Extensions["stackTrace"] = exception.StackTrace;
        }

        return problemDetails;
    }

    public bool CanHandle(Exception exception)
    {
        // This factory can handle any exception
        return true;
    }

    public int GetStatusCode(Exception exception)
    {
        return ExceptionMappings.GetStatusCode(exception.GetType());
    }

    /// <summary>
    /// Creates ValidationProblemDetails for FluentValidation exceptions
    /// </summary>
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
    /// Gets a safe message for the given exception, avoiding sensitive information exposure
    /// </summary>
    private static string? GetSafeMessage(Exception exception, int statusCode)
    {
        return statusCode switch
        {
            401 => "Access denied",
            403 => "Forbidden",
            404 => "Resource not found",
            409 => "Resource conflict",
            422 => "Unprocessable entity",
            >= 400 and < 500 => "Client error occurred",
            >= 500 => "An internal server error occurred",
            _ => null
        };
    }
}