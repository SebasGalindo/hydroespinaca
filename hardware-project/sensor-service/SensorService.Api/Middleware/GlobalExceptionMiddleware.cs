using FluentValidation;
using HydroEspinaca.Shared.Errors;
using HydroEspinaca.Shared.Responses;
using System.Net;
using System.Text.Json;

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
        catch (ValidationException ex)
        {
            _logger.LogWarning("⚠️ Validation error: {Errors}", ex.Errors);

            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            context.Response.ContentType = "application/json";

            var error = new ErrorResponse
            {
                Message = "Validation failed",
                Detail = _env.IsDevelopment() ? "One or more validation errors occurred." : null,
                Errors = ex.Errors.Select(e => e.ErrorMessage).ToList()
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(error));
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning("⚠️ Not found: {Message}", ex.Message);

            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            context.Response.ContentType = "application/json";

            var error = new ErrorResponse
            {
                Message = ex.Message,
                Detail = _env.IsDevelopment() ? "The requested resource was not found." : null
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(error));
        }

        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("🔐 Unauthorized access: {Message}", ex.Message);

            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            context.Response.ContentType = "application/json";

            var error = new ErrorResponse
            {
                Message = "Unauthorized",
                Detail = _env.IsDevelopment() ? ex.Message : null
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(error));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Unhandled exception");

            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";

            var error = new ErrorResponse
            {
                Message = "Internal Server Error",
                Detail = _env.IsDevelopment() || _env.IsStaging() ? ex.Message : null,
                Stack = _env.IsDevelopment() || _env.IsStaging() ? ex.StackTrace : null
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(error));
        }
    }

}