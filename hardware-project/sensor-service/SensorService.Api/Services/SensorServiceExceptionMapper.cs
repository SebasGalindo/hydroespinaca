using HydroEspinaca.Shared.Authentication.Interfaces;
using HydroEspinaca.Shared.Errors;
using Microsoft.AspNetCore.Mvc;
using SensorDomainException = SensorService.Domain.Exceptions.DomainException;
using SensorService.Domain.Exceptions;

namespace SensorService.Api.Services;

/// <summary>
/// Maps domain and application exceptions to appropriate HTTP status codes and error responses.
/// </summary>
public class SensorServiceExceptionMapper : IExceptionToProblemDetailsMapper
{
    private readonly ProblemDetailsFactory _sharedMapper;

    public SensorServiceExceptionMapper(ProblemDetailsFactory sharedMapper)
    {
        _sharedMapper = sharedMapper;
    }

    public ProblemDetails MapToProblemDetails(Exception exception, string requestPath, bool isDevelopment)
    {
        return exception switch
        {
            // Domain-specific exceptions
            InvalidTimeWindowException ex => CreateProblemDetails(
                "Invalid Time Window", ex.Message, 400,
                "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                requestPath, isDevelopment),

            SensorDomainException ex => CreateProblemDetails(
                "Domain Error", ex.Message, 400,
                "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                requestPath, isDevelopment),

            // Delegate to shared mapper for standard exceptions
            _ => _sharedMapper.MapToProblemDetails(exception, requestPath, isDevelopment)
        };
    }

    public bool CanHandle(Exception exception)
    {
        return exception is SensorDomainException ||
               _sharedMapper.CanHandle(exception);
    }

    public int GetStatusCode(Exception exception)
    {
        return exception switch
        {
            InvalidTimeWindowException => 400,
            SensorDomainException => 400,
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

    private static string GetSafeMessage(int statusCode)
    {
        return statusCode switch
        {
            401 => "Access denied",
            403 => "Forbidden",
            400 => "Invalid request",
            404 => "Resource not found",
            _ => "An error occurred"
        };
    }
}