using FluentValidation;
using HydroEspinaca.Shared.Errors;
using Microsoft.AspNetCore.Mvc;
using ActuatorService.Api.Helpers;
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

            var problem = ex.ToProblemDetails(context);
            await context.WriteProblemDetailsAsync(problem, StatusCodes.Status400BadRequest);
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning("⚠️ Not found: {Message}", ex.Message);

            var problem = ProblemDetailsHelper.Create(context,
                title: "Resource Not Found",
                detail: ex.Message,
                statusCode: StatusCodes.Status404NotFound,
                type: "https://tools.ietf.org/html/rfc9110#section-15.5.5",
                env: _env);

            await context.WriteProblemDetailsAsync(problem, StatusCodes.Status404NotFound);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("🔐 Unauthorized access: {Message}", ex.Message);

            var problem = ProblemDetailsHelper.Create(context,
                title: "Unauthorized",
                detail: ex.Message,
                statusCode: StatusCodes.Status401Unauthorized,
                type: "https://tools.ietf.org/html/rfc9110#section-15.5.2",
                env: _env);

            await context.WriteProblemDetailsAsync(problem, StatusCodes.Status401Unauthorized);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("⚠️ Invalid argument: {Message}", ex.Message);
            var problem = ProblemDetailsHelper.Create(context,
                title: "Invalid Parameter",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest,
                type: "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                env: _env);
            await context.WriteProblemDetailsAsync(problem, StatusCodes.Status400BadRequest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Unhandled exception");

            var problem = ProblemDetailsHelper.Create(context,
                title: "Internal Server Error",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError,
                type: "https://tools.ietf.org/html/rfc9110#section-15.6.1",
                env: _env);

            if (_env.IsDevelopment() || _env.IsStaging())
                problem.Extensions["stackTrace"] = ex.StackTrace;

            await context.WriteProblemDetailsAsync(problem, StatusCodes.Status500InternalServerError);
        }
    }
}
