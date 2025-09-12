using BffService.Domain.Exceptions;
using FluentValidation;
using HydroEspinaca.Shared.Authentication.Interfaces;
using HydroEspinaca.Shared.Errors;
using Microsoft.AspNetCore.Mvc;

namespace BffService.Api.Services;

/// <summary>
/// BFF service specific exception mapper that extends the shared mapper
/// </summary>
public class BffServiceExceptionMapper : IExceptionToProblemDetailsMapper
{
    private readonly ProblemDetailsFactory _sharedMapper;

    public BffServiceExceptionMapper(ProblemDetailsFactory sharedMapper)
    {
        _sharedMapper = sharedMapper;
    }

    public ProblemDetails MapToProblemDetails(Exception exception, string requestPath, bool isDevelopment)
    {
        // Handle BFF-service specific exceptions with custom titles/messages
        return exception switch
        {
            SessionExpiredException ex => CreateProblemDetails(
                "Session Expired", ex.Message, 401, 
                "https://tools.ietf.org/html/rfc9110#section-15.5.2", 
                requestPath, isDevelopment),
                
            InvalidTokenException ex => CreateProblemDetails(
                "Invalid Token", ex.Message, 401, 
                "https://tools.ietf.org/html/rfc9110#section-15.5.2", 
                requestPath, isDevelopment),
            
            ValidationException ex => CreateValidationProblemDetails(ex, requestPath),
            
            // Delegate to shared mapper for all other exceptions (including SessionNotFoundException, ProxyException)
            // These will be automatically handled by the shared mapper through inheritance
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
            // Only override status codes where we have specific business logic
            SessionExpiredException => 401,
            InvalidTokenException => 401,
            ValidationException => 400,
            _ => _sharedMapper.GetStatusCode(exception)
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

    private static string GetSafeMessage(int statusCode)
    {
        return statusCode switch
        {
            401 => "Authentication required",
            404 => "Session not found",
            502 => "Service temporarily unavailable",
            400 => "Invalid request",
            _ => "An error occurred"
        };
    }
}