using AuthService.Api.Helpers;
using AuthService.Application.Exceptions;
using AuthService.Domain.Exceptions;
using FluentValidation;
using HydroEspinaca.Shared.Errors;

namespace AuthService.Api.Middleware;

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
        catch (InvalidClientCredentialsException ex)
        {
            _logger.LogWarning("🔐 Invalid client credentials: {Message}", ex.Message);

            var problem = ProblemDetailsHelper.Create(context,
                title: "Unauthorized",
                detail: ex.Message,
                statusCode: StatusCodes.Status401Unauthorized,
                type: "https://tools.ietf.org/html/rfc9110#section-15.5.2",
                env: _env);

            await context.WriteProblemDetailsAsync(problem, StatusCodes.Status401Unauthorized);
        }

        catch (TokenExpiredException ex)
        {
            _logger.LogWarning("⏳ Token expired: {Message}", ex.Message);

            var problem = ProblemDetailsHelper.Create(context,
                title: "Unauthorized",
                detail: ex.Message,
                statusCode: StatusCodes.Status401Unauthorized,
                type: "https://tools.ietf.org/html/rfc9110#section-15.5.2",
                env: _env);

            await context.WriteProblemDetailsAsync(problem, StatusCodes.Status401Unauthorized);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning("⚠️ Domain exception: {Message}", ex.Message);

            var problem = ProblemDetailsHelper.Create(context,
                title: "Domain Error",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest,
                type: "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                env: _env);

            await context.WriteProblemDetailsAsync(problem, StatusCodes.Status400BadRequest);
        }

        catch (ClientAppAlreadyExistsException ex)
        {
            _logger.LogWarning("⚠️ Client app already exists: {Message}", ex.Message);

            var problem = ProblemDetailsHelper.Create(context,
                title: "Conflict",
                detail: ex.Message,
                statusCode: StatusCodes.Status409Conflict,
                type: "https://tools.ietf.org/html/rfc9110#section-15.5.8",
                env: _env);

            await context.WriteProblemDetailsAsync(problem, StatusCodes.Status409Conflict);
        }

        catch (InvalidRefreshTokenException ex)
        {
            _logger.LogWarning("♻️ Invalid refresh token: {Message}", ex.Message);

            var problem = ProblemDetailsHelper.Create(context,
                title: "Unauthorized",
                detail: ex.Message,
                statusCode: StatusCodes.Status401Unauthorized,
                type: "https://tools.ietf.org/html/rfc9110#section-15.5.2",
                env: _env);

            await context.WriteProblemDetailsAsync(problem, StatusCodes.Status401Unauthorized);
        }

        catch (InvalidCredentialsException ex)
        {
            _logger.LogWarning("🔐 Invalid credentials: {Message}", ex.Message);

            var problem = ProblemDetailsHelper.Create(context,
                title: "Unauthorized",
                detail: ex.Message,
                statusCode: StatusCodes.Status401Unauthorized,
                type: "https://tools.ietf.org/html/rfc9110#section-15.5.2",
                env: _env);

            await context.WriteProblemDetailsAsync(problem, StatusCodes.Status401Unauthorized);
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
