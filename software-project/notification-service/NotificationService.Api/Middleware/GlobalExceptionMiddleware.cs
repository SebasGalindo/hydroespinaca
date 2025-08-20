using FluentValidation;
using NotificationService.Api.Helpers;

namespace NotificationService.Api.Middleware;

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
            _logger.LogWarning("Validation failed: {Count} errors", ex.Errors?.Count() ?? 0);
            var problem = ex.ToProblemDetails(context);
            await context.WriteProblemDetailsAsync(problem, StatusCodes.Status400BadRequest);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Bad argument: {Msg}", ex.Message);
            var problem = ProblemDetailsHelper.Create(context, "Invalid Parameter", ex.Message, StatusCodes.Status400BadRequest, "about:blank", _env);
            await context.WriteProblemDetailsAsync(problem, StatusCodes.Status400BadRequest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            var detail = _env.IsDevelopment() ? ex.ToString() : ex.Message;
            var problem = ProblemDetailsHelper.Create(context, "Internal Server Error", detail, StatusCodes.Status500InternalServerError, "about:blank", _env);
            await context.WriteProblemDetailsAsync(problem, StatusCodes.Status500InternalServerError);
        }
    }
}
