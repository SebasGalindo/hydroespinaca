using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace SensorService.Api.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            // Logea excepción completa
            _logger.LogError(ex, "❌ Unhandled exception processing {Method} {Path}",
                             context.Request.Method, context.Request.Path);

            // Devuelve JSON con detalle de error
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";
            var payload = JsonSerializer.Serialize(new
            {
                message = "Internal Server Error",
                detail = ex.Message,
                stack = ex.StackTrace
            });
            await context.Response.WriteAsync(payload);
        }
    }
}
