using FluentValidation;
using HydroEspinaca.Shared.Errors;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;

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
            _logger.LogWarning("⚠️ Validation failed. Error count: {ErrorCount}", ex.Errors?.Count() ?? 0);

            if (ex.Errors != null)
            {
                foreach (var error in ex.Errors)
                {
                    _logger.LogWarning("Validation error - Property: {Property}, Error: {Error}",
                        error.PropertyName ?? "Unknown", error.ErrorMessage);
                }
            }

            var problemDetails = new ProblemDetails
            {
                Title = "Validation Failed",
                Status = StatusCodes.Status400BadRequest,
                Detail = "One or more validation errors occurred.",
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1"
            };
            
            problemDetails.Extensions["errors"] = ex.Errors?.Select(e => new { property = e.PropertyName, message = e.ErrorMessage }).ToArray() ?? Array.Empty<object>();

            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(problemDetails);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Unhandled exception occurred");

            var problem = new ProblemDetails
            {
                Title = "An error occurred",
                Detail = _env.IsDevelopment() ? ex.Message : "An internal server error occurred",
                Status = StatusCodes.Status500InternalServerError,
                Type = "https://tools.ietf.org/html/rfc9110#section-15.6.1"
            };

            if (_env.IsDevelopment())
                problem.Extensions["stackTrace"] = ex.StackTrace;

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(problem);
        }
    }
}
