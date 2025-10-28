using Microsoft.AspNetCore.Mvc;
using BffService.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace BffService.Api.Helpers;

/// <summary>
/// Helper class to centralize exception handling logic across controllers.
/// Reduces code duplication by providing consistent error responses.
/// </summary>
public static class ControllerExceptionHandler
{
    /// <summary>
    /// Handles common exceptions and returns appropriate IActionResult.
    /// </summary>
    /// <param name="exception">The exception to handle</param>
    /// <param name="logger">Logger instance for logging</param>
    /// <param name="context">Optional context for logging (e.g., "getting users", "creating role")</param>
    /// <param name="identifier">Optional identifier for the resource (e.g., userId, roleCode)</param>
    /// <param name="httpContext">Optional HttpContext for cookie cleanup</param>
    /// <returns>IActionResult with appropriate status code and message</returns>
    public static IActionResult HandleException(
        Exception exception,
        ILogger logger,
        string? context = null,
        string? identifier = null,
        HttpContext? httpContext = null)
    {
        return exception switch
        {
            SessionNotFoundException ex => HandleSessionNotFound(ex, logger, identifier),
            SessionExpiredException ex => HandleSessionExpired(ex, logger, identifier, httpContext),
            InvalidTokenException ex => HandleInvalidToken(ex, logger, identifier),
            ServiceException ex => HandleServiceException(ex, logger, context, identifier),
            InvalidOperationException ex => HandleInvalidOperation(ex, logger, context),
            UnauthorizedAccessException ex => HandleUnauthorizedAccess(ex, logger, identifier),
            HttpRequestException ex => HandleHttpRequestException(ex, logger, context, identifier),
            OperationCanceledException ex => HandleOperationCanceled(ex, logger),
            _ => HandleGenericException(exception, logger, context, identifier)
        };
    }

    private static UnauthorizedObjectResult HandleSessionNotFound(SessionNotFoundException ex, ILogger logger, string? sessionId)
    {
        logger.LogWarning(ex, "Session not found: {SessionId}", sessionId);
        return new UnauthorizedObjectResult(new { message = "Session not found" });
    }

    private static UnauthorizedObjectResult HandleSessionExpired(
        SessionExpiredException ex,
        ILogger logger,
        string? sessionId,
        HttpContext? httpContext = null)
    {
        logger.LogWarning(ex, "Session expired and cannot be refreshed: {SessionId}", sessionId);

        // Clear cookies if HttpContext is available (session is truly expired, not just missing)
        if (httpContext != null)
        {
            ClearSessionCookies(httpContext, logger);
        }

        return new UnauthorizedObjectResult(new { message = "Session expired, please login again" });
    }

    /// <summary>
    /// Clears session cookies by setting them to expire in the past.
    /// </summary>
    private static void ClearSessionCookies(HttpContext httpContext, ILogger logger)
    {
        try
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Path = "/",
                Expires = DateTimeOffset.UtcNow.AddDays(-1) // Expire in the past
            };

            httpContext.Response.Cookies.Append("SessionId", "", cookieOptions);

            var csrfCookieOptions = new CookieOptions
            {
                HttpOnly = false,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Path = "/",
                Expires = DateTimeOffset.UtcNow.AddDays(-1)
            };

            httpContext.Response.Cookies.Append("CsrfToken", "", csrfCookieOptions);

            logger.LogInformation("Session cookies cleared due to expiration");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to clear session cookies");
        }
    }

    private static UnauthorizedObjectResult HandleInvalidToken(InvalidTokenException ex, ILogger logger, string? sessionId)
    {
        logger.LogWarning(
            ex,
            "Invalid token exception for session {SessionId} - this should NOT happen if auto-refresh works correctly",
            sessionId);
        return new UnauthorizedObjectResult(new { message = "Session is no longer valid, please login again" });
    }

    private static ObjectResult HandleServiceException(ServiceException ex, ILogger logger, string? context, string? identifier)
    {
        logger.LogWarning(ex, "Service error: {Context}, Identifier: {Identifier}, StatusCode: {StatusCode}", context, identifier, ex.StatusCode);
        return new ObjectResult(new { message = ex.Message })
        {
            StatusCode = ex.StatusCode
        };
    }

    private static BadRequestObjectResult HandleInvalidOperation(InvalidOperationException ex, ILogger logger, string? context)
    {
        logger.LogWarning(ex, "Invalid operation: {Context}", context);
        return new BadRequestObjectResult(new { message = ex.Message });
    }

    private static ObjectResult HandleUnauthorizedAccess(UnauthorizedAccessException ex, ILogger logger, string? sessionId)
    {
        logger.LogError(
            ex,
            "⚠️  MICROSERVICE AUTHORIZATION FAILED: One or more microservices rejected the JWT token (403 Forbidden) | SessionId: {SessionId}",
            sessionId);
        return new ObjectResult(new
        {
            message = "System status unavailable - microservices are not accepting authentication tokens",
            error = "SERVICE_UNAVAILABLE",
            details = "The downstream microservices are rejecting JWT tokens. This is a configuration issue that needs to be resolved in the microservices.",
            retryAfter = 60
        })
        { StatusCode = 503 };
    }

    private static IActionResult HandleHttpRequestException(HttpRequestException ex, ILogger logger, string? context, string? identifier)
    {
        logger.LogWarning(ex, "HTTP request error: {Context}, Identifier: {Identifier}", context, identifier);

        // Different handling based on context
        if (context?.Contains("deleting") == true || context?.Contains("revoking") == true)
        {
            return new NotFoundObjectResult(new { message = $"Resource not found or already {context.Split(' ')[0]}d" });
        }

        return new BadRequestObjectResult(new { message = $"Failed to {context ?? "process request"}" });
    }

    private static ObjectResult HandleOperationCanceled(OperationCanceledException ex, ILogger logger)
    {
        logger.LogWarning(ex, "Request timeout");
        return new ObjectResult(new { message = "Request timeout - services not responding" })
        { StatusCode = 504 };
    }

    private static ObjectResult HandleGenericException(Exception ex, ILogger logger, string? context, string? identifier)
    {
        logger.LogError(ex, "Error {Context}, Identifier: {Identifier}", context ?? "processing request", identifier);
        return new ObjectResult(new { message = "Internal server error" })
        { StatusCode = 500 };
    }

    /// <summary>
    /// Executes an async action with centralized exception handling.
    /// </summary>
    /// <typeparam name="T">The return type of the action</typeparam>
    /// <param name="action">The action to execute</param>
    /// <param name="logger">Logger instance</param>
    /// <param name="context">Context for logging</param>
    /// <param name="identifier">Optional identifier</param>
    /// <returns>IActionResult - either success result or handled exception</returns>
    public static async Task<IActionResult> ExecuteWithHandling<T>(
        Func<Task<T>> action,
        ILogger logger,
        string context,
        string? identifier = null) where T : IActionResult
    {
        try
        {
            return await action();
        }
        catch (Exception ex)
        {
            return HandleException(ex, logger, context, identifier);
        }
    }
}
