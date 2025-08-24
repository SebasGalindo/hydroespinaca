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
            _logger.LogError(ex, "❌ Unhandled exception in auth-service");

            var problemDetails = _exceptionMapper.MapToProblemDetails(
                ex, 
                context.Request.Path, 
                _env.IsDevelopment()
            );

            var statusCode = _exceptionMapper.GetStatusCode(ex);
            
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/problem+json";
            
            await context.Response.WriteAsJsonAsync(problemDetails);
        }
    }
}
