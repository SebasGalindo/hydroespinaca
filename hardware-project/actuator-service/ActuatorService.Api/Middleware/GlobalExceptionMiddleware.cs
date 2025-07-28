using FluentValidation;
using HydroEspinaca.Shared.Errors;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text.Json;

namespace ActuatorService.Api.Middleware;

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
            _logger.LogWarning("⚠️ Validation failed: {Errors}", ex.Errors);

            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            context.Response.ContentType = "application/problem+json";

            var validationErrors = ex.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray()
                );

            var problem = new ValidationProblemDetails(validationErrors)
            {
                Title = "One or more validation errors occurred.",
                Status = StatusCodes.Status400BadRequest,
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                Instance = context.Request.Path
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning("⚠️ Not found: {Message}", ex.Message);

            context.Response.StatusCode = StatusCodes.Status404NotFound;
            context.Response.ContentType = "application/problem+json";

            var problem = new ProblemDetails
            {
                Title = "Resource Not Found",
                Detail = _env.IsDevelopment() ? ex.Message : null,
                Status = StatusCodes.Status404NotFound,
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.5",
                Instance = context.Request.Path
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("🔐 Unauthorized access: {Message}", ex.Message);

            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/problem+json";

            var problem = new ProblemDetails
            {
                Title = "Unauthorized",
                Detail = _env.IsDevelopment() ? ex.Message : null,
                Status = StatusCodes.Status401Unauthorized,
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.2",
                Instance = context.Request.Path
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Unhandled exception");

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/problem+json";

            var problem = new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = _env.IsDevelopment() || _env.IsStaging() ? ex.Message : null,
                Status = StatusCodes.Status500InternalServerError,
                Type = "https://tools.ietf.org/html/rfc9110#section-15.6.1",
                Instance = context.Request.Path
            };

            if (_env.IsDevelopment() || _env.IsStaging())
                problem.Extensions["stackTrace"] = ex.StackTrace;

            await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
        }
    }
}
