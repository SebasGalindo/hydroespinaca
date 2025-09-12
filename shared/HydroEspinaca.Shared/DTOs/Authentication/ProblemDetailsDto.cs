using System.Text.Json.Serialization;

namespace HydroEspinaca.Shared.DTOs.Authentication;

/// <summary>
/// DTO for HTTP context information needed for exception handling
/// </summary>
public class ExceptionContextDto
{
    public string RequestPath { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public bool IsDevelopment { get; set; }
    public string? TraceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
}

/// <summary>
/// Extension methods for working with ProblemDetails in a framework-agnostic way
/// </summary>
public static class ProblemDetailsExtensions
{
    /// <summary>
    /// Converts exception to JSON string for HTTP response
    /// </summary>
    public static string ToJsonString(this Exception exception, ExceptionContextDto context)
    {
        var problemDetails = new
        {
            type = GetProblemType(exception),
            title = GetTitle(exception),
            status = GetStatusCode(exception),
            detail = context.IsDevelopment ? exception.Message : GetSafeMessage(exception),
            instance = context.RequestPath,
            traceId = context.TraceId,
            service = context.ServiceName
        };

        return System.Text.Json.JsonSerializer.Serialize(problemDetails, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
        });
    }

    private static string GetProblemType(Exception exception)
    {
        return exception.GetType().Name switch
        {
            nameof(UnauthorizedAccessException) => "https://tools.ietf.org/html/rfc9110#section-15.5.2",
            nameof(ArgumentException) => "https://tools.ietf.org/html/rfc9110#section-15.5.1",
            nameof(ArgumentNullException) => "https://tools.ietf.org/html/rfc9110#section-15.5.1",
            _ => "https://tools.ietf.org/html/rfc9110#section-15.6.1"
        };
    }

    private static string GetTitle(Exception exception)
    {
        return exception.GetType().Name switch
        {
            nameof(UnauthorizedAccessException) => "Unauthorized",
            nameof(ArgumentException) => "Invalid Parameter",
            nameof(ArgumentNullException) => "Invalid Parameter",
            _ => "Internal Server Error"
        };
    }

    private static int GetStatusCode(Exception exception)
    {
        return exception.GetType().Name switch
        {
            nameof(UnauthorizedAccessException) => 401,
            nameof(ArgumentException) => 400,
            nameof(ArgumentNullException) => 400,
            _ => 500
        };
    }

    private static string GetSafeMessage(Exception exception)
    {
        return GetStatusCode(exception) switch
        {
            401 => "Access denied",
            400 => "Invalid request",
            _ => "An internal server error occurred"
        };
    }
}