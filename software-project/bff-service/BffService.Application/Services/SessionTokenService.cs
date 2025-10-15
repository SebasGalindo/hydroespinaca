using BffService.Application.Interfaces;
using BffService.Domain.Entities;
using BffService.Domain.Exceptions;
using BffService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace BffService.Application.Services;

/// <summary>
/// Service for managing session tokens including automatic refresh
/// </summary>
public class SessionTokenService : ISessionTokenService
{
    private readonly ISessionService _sessionService;
    private readonly IAuthService _authService;
    private readonly ILogger<SessionTokenService> _logger;

    public SessionTokenService(
        ISessionService sessionService,
        IAuthService authService,
        ILogger<SessionTokenService> logger)
    {
        _sessionService = sessionService;
        _authService = authService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Session> GetSessionWithValidTokensAsync(string sessionId, CancellationToken cancellationToken)
    {
        // Get the full session with all token information
        var session = await _sessionService.GetFullSessionAsync(sessionId, cancellationToken);
        if (session == null)
        {
            _logger.LogWarning("Session not found: {SessionId}", sessionId);
            throw new SessionNotFoundException(sessionId);
        }

        if (!session.HasValidTokens())
        {
            _logger.LogWarning("Session has invalid tokens: {SessionId}", sessionId);
            throw new InvalidTokenException("Session tokens are invalid");
        }

        // Check if the access token is expired and needs refreshing
        if (session.IsExpired() && session.CanRefresh())
        {
            _logger.LogInformation("Access token expired for session {SessionId}, attempting refresh", sessionId);

            // Use the auth service to refresh the token
            var newTokenInfo = await _authService.RefreshTokenAsync(session.RefreshToken, cancellationToken);

            // Update the session with the new tokens
            session.UpdateAccessToken(newTokenInfo.AccessToken, newTokenInfo.ExpiresAt);

            // If we got a new refresh token, update that too
            if (!string.IsNullOrEmpty(newTokenInfo.RefreshToken))
            {
                session.SetTokens(
                    newTokenInfo.AccessToken,
                    newTokenInfo.RefreshToken,
                    newTokenInfo.ExpiresAt,
                    newTokenInfo.RefreshTokenExpiresAt
                );
            }

            // Save the updated session
            await _sessionService.UpdateSessionAsync(session, cancellationToken);

            _logger.LogInformation("Successfully refreshed tokens for session: {SessionId}", sessionId);
        }
        else if (session.IsExpired())
        {
            _logger.LogWarning("Session {SessionId} is expired and cannot be refreshed", sessionId);
            throw new SessionExpiredException(sessionId);
        }

        return session;
    }
}
