using System.Net;
using System.Text.Json;
using HydroEspinaca.Shared.Responses;

namespace SensorService.Api.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IWebHostEnvironment _env;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger, IWebHostEnvironment env)
    {
        _next = next;
        _logger = logger;
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
            _logger.LogError(ex, "❌ Unhandled exception");

            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";

            var error = new ErrorResponse
            {
                Message = "Internal Server Error"
            };

            if (_env.IsDevelopment() || _env.IsStaging())
            {
                error.Detail = ex.Message;
                error.Stack = ex.StackTrace;
            }

            await context.Response.WriteAsync(JsonSerializer.Serialize(error));

        }
    }
}
