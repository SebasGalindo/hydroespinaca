using Microsoft.AspNetCore.Mvc;
using BffService.Domain.Constants;
using BffService.Domain.Exceptions;
using BffService.Domain.Interfaces;

namespace BffService.Api.Controllers;

/// <summary>
/// Base controller for authenticated endpoints that require session validation.
/// Provides shared session validation logic to avoid code duplication.
/// </summary>
public abstract class BaseAuthenticatedController : ControllerBase
{
    protected readonly ISessionTokenService SessionTokenService;
    protected readonly IConfiguration Configuration;
    protected readonly ILogger Logger;

    protected BaseAuthenticatedController(
        ISessionTokenService sessionTokenService,
        IConfiguration configuration,
        ILogger logger)
    {
        SessionTokenService = sessionTokenService;
        Configuration = configuration;
        Logger = logger;
    }

    /// <summary>
    /// Validates the current request's session and returns a session with valid tokens.
    /// Throws SessionNotFoundException or SessionExpiredException if validation fails.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Session with valid access and refresh tokens</returns>
    /// <exception cref="SessionNotFoundException">Session ID not found in request</exception>
    /// <exception cref="SessionExpiredException">Session has expired</exception>
    protected async Task<Domain.Entities.Session> ValidateSessionAsync(CancellationToken cancellationToken)
    {
        var sessionId = GetSessionIdFromRequest();

        if (string.IsNullOrWhiteSpace(sessionId))
        {
            throw new SessionNotFoundException("Session ID not found in request");
        }

        // Ensure we have a valid access token (refresh if needed)
        return await SessionTokenService.GetSessionWithValidTokensAsync(sessionId, cancellationToken);
    }

    /// <summary>
    /// Extracts session ID from the current request (cookie or header).
    /// Returns null if not found.
    /// </summary>
    /// <returns>Session ID or null</returns>
    protected string? GetSessionIdFromRequest()
    {
        var sessionIdHeaderKey = Configuration[BffConstants.Sessions.SessionIdHeaderConfigKey] ?? "X-Session-Id";

        // Try to read from cookie (web)
        if (Request.Cookies.TryGetValue("SessionId", out var cookieSessionId))
        {
            return cookieSessionId;
        }

        // Fallback to header (mobile or external clients)
        if (Request.Headers.TryGetValue(sessionIdHeaderKey, out var headerSessionId))
        {
            return headerSessionId.FirstOrDefault();
        }

        return null;
    }
}
