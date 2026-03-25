using HydroEspinaca.Shared.Authentication.Interfaces;

namespace BiService.Api.Middleware;

/// <summary>
/// Global exception middleware using shared exception mapping logic.
/// Catches all unhandled exceptions and returns RFC 7807 ProblemDetails.
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

            LogException(ex, statusCode, context.Request.Path);

            var problemDetails = _exceptionMapper.MapToProblemDetails(
                ex,
                context.Request.Path,
                _env.IsDevelopment());

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/problem+json";

            await context.Response.WriteAsJsonAsync(problemDetails);
        }
    }

    private void LogException(Exception ex, int statusCode, string requestPath)
    {
        var exceptionType = ex.GetType().Name;

        if (statusCode >= 400 && statusCode < 500)
        {
            _logger.LogWarning(
                ex,
                "⚠️  Client error in bi-service: {ExceptionType} at {Path} (Status: {StatusCode})",
                exceptionType, requestPath, statusCode);
        }
        else
        {
            _logger.LogError(
                ex,
                "❌ Unexpected exception in bi-service: {ExceptionType} at {Path} (Status: {StatusCode})",
                exceptionType, requestPath, statusCode);
        }
    }
}
