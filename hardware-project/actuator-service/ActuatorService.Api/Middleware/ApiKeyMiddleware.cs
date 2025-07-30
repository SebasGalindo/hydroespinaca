using Microsoft.Extensions.Options;
using HydroEspinaca.Shared.Options;

namespace ActuatorService.Api.Middleware;

public class ApiKeyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _expectedApiKey;

    public ApiKeyMiddleware(RequestDelegate next, IOptions<ApiKeySettings> options)
    {
        _next = next;
        _expectedApiKey = options.Value.Key;
    }

    public async Task Invoke(HttpContext context)
    {
        var path = context.Request.Path.Value;

        if (string.IsNullOrEmpty(path))
        {
            context.Response.StatusCode = 400; // Bad Request
            await context.Response.WriteAsync("Path cannot be empty");
            return;
        }

        if (path.StartsWith("/swagger") || path.StartsWith("/docs") || path == "/")
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue("X-API-Key", out var extractedApiKey) ||
            extractedApiKey != _expectedApiKey)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Unauthorized");
            return;
        }

        await _next(context);
    }
}

