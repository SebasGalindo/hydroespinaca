using HydroEspinaca.Shared.Authentication.Interfaces;

namespace AuthService.Api.Middleware;

/// <summary>
/// Simplified global exception middleware using shared exception mapping logic
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IExceptionToProblemDetailsMapper _exceptionMapper;
    private readonly IWebHostEnvironment _env;

    public GlobalExceptionMiddleware(
        RequestDelegate next, 
        ILogger<GlobalExceptionMiddleware> logger,
        IExceptionToProblemDetailsMapper exceptionMapper,
        IWebHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _exceptionMapper = exceptionMapper;
        _env = env;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var statusCode = _exceptionMapper.GetStatusCode(ex);

            // Log with appropriate severity based on error type
            LogException(ex, statusCode, context.Request.Path);

            var problemDetails = _exceptionMapper.MapToProblemDetails(
                ex,
                context.Request.Path,
                _env.IsDevelopment()
            );

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/problem+json";

            await context.Response.WriteAsJsonAsync(problemDetails);
        }
    }

    /// <summary>
    /// Logs exceptions with appropriate severity level based on HTTP status code
    /// </summary>
    private void LogException(Exception ex, int statusCode, string requestPath)
    {
        var exceptionType = ex.GetType().Name;

        if (IsExpectedClientError(statusCode))
        {
            // Expected business/validation errors (4xx) - log as warning
            _logger.LogWarning(
                ex,
                "⚠️  Expected client error in auth-service: {ExceptionType} at {Path} (Status: {StatusCode})",
                exceptionType,
                requestPath,
                statusCode
            );
        }
        else
        {
            // Unexpected server errors (5xx) or other errors - log as error
            _logger.LogError(
                ex,
                "❌ Unexpected exception in auth-service: {ExceptionType} at {Path} (Status: {StatusCode})",
                exceptionType,
                requestPath,
                statusCode
            );
        }
    }

    /// <summary>
    /// Determines if an HTTP status code represents an expected client error
    /// </summary>
    private static bool IsExpectedClientError(int statusCode)
    {
        // 4xx status codes are client errors and generally expected
        return statusCode >= 400 && statusCode < 500;
    }
}
