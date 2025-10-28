using AuthService.Application.Exceptions;
using AuthService.Domain.Exceptions;
using FluentValidation;
using HydroEspinaca.Shared.Authentication.Interfaces;
using HydroEspinaca.Shared.Errors;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Api.Services;

/// <summary>
/// Auth service specific exception mapper that extends the shared mapper
/// </summary>
public class AuthServiceExceptionMapper : IExceptionToProblemDetailsMapper
{
    private readonly ProblemDetailsFactory _sharedMapper;

    public AuthServiceExceptionMapper(ProblemDetailsFactory sharedMapper)
    {
        _sharedMapper = sharedMapper;
    }

    public ProblemDetails MapToProblemDetails(Exception exception, string requestPath, bool isDevelopment)
    {
        // Handle auth-service specific exceptions
        return exception switch
        {
            InvalidClientCredentialsException ex => CreateProblemDetails(
                "Unauthorized", ex.Message, 401,
                "https://tools.ietf.org/html/rfc9110#section-15.5.2",
                requestPath, isDevelopment),

            TokenExpiredException ex => CreateProblemDetails(
                "Unauthorized", ex.Message, 401,
                "https://tools.ietf.org/html/rfc9110#section-15.5.2",
                requestPath, isDevelopment),

            InvalidRefreshTokenException ex => CreateProblemDetails(
                "Unauthorized", ex.Message, 401,
                "https://tools.ietf.org/html/rfc9110#section-15.5.2",
                requestPath, isDevelopment),

            InvalidCredentialsException ex => CreateProblemDetails(
                "Unauthorized", ex.Message, 401,
                "https://tools.ietf.org/html/rfc9110#section-15.5.2",
                requestPath, isDevelopment),

            ClientAppAlreadyExistsException ex => CreateProblemDetails(
                "Conflict", ex.Message, 409,
                "https://tools.ietf.org/html/rfc9110#section-15.5.8",
                requestPath, isDevelopment),

            RoleHasAssignedUsersException ex => CreateProblemDetails(
                "Bad Request", ex.Message, 400,
                "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                requestPath, isDevelopment),

            ValidationException ex => CreateValidationProblemDetails(ex, requestPath),

            // Delegate to shared mapper for all other exceptions
            _ => _sharedMapper.MapToProblemDetails(exception, requestPath, isDevelopment)
        };
    }

    public bool CanHandle(Exception exception)
    {
        return exception is InvalidClientCredentialsException or
               TokenExpiredException or
               InvalidRefreshTokenException or
               InvalidCredentialsException or
               ClientAppAlreadyExistsException or
               RoleHasAssignedUsersException or
               ValidationException ||
               _sharedMapper.CanHandle(exception);
    }

    public int GetStatusCode(Exception exception)
    {
        return exception switch
        {
            InvalidClientCredentialsException => 401,
            TokenExpiredException => 401,
            InvalidRefreshTokenException => 401,
            InvalidCredentialsException => 401,
            ClientAppAlreadyExistsException => 409,
            RoleHasAssignedUsersException => 400,
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
            401 => "Access denied",
            409 => "Resource conflict",
            400 => "Invalid request",
            _ => "An error occurred"
        };
    }
}