using BffService.Application.Interfaces;
using BffService.Domain.Entities;
using BffService.Domain.Interfaces;
using BffService.Domain.Services;
using BffService.Domain.Exceptions;
using HydroEspinaca.Shared.DTOs.Authentication;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace BffService.Application.Services;

public class SessionApplicationService : ISessionService
{
    private readonly ISessionRepository _sessionRepository;
    private readonly IAuthService _authService;
    private readonly SessionValidationService _validationService;
    private readonly ILogger<SessionApplicationService> _logger;

    public SessionApplicationService(
        ISessionRepository sessionRepository,
        IAuthService authService,
        SessionValidationService validationService,
        ILogger<SessionApplicationService> logger)
    {
        _sessionRepository = sessionRepository;
        _authService = authService;
        _validationService = validationService;
        _logger = logger;
    }

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Attempting login for user: {Email}", request.Email);

        // Generate session credentials first
        var sessionId = GenerateSessionId();
        var csrfToken = GenerateCsrfToken();

        // Pass sessionId and csrfToken to auth service so it can store them
        var authResult = await _authService.LoginAsync(
            request.Email,
            request.Password,
            sessionId,
            csrfToken,
            cancellationToken);

        var session = Session.Create(sessionId, csrfToken);
        session.SetTokens(
            authResult.TokenInfo.AccessToken,
            authResult.TokenInfo.RefreshToken,
            authResult.TokenInfo.ExpiresAt,
            authResult.TokenInfo.RefreshTokenExpiresAt
        );
        session.SetUserInfo(authResult.UserId, authResult.Username, authResult.Email, authResult.UserRole, authResult.Scopes);

        await _sessionRepository.SaveAsync(session, cancellationToken);

        _logger.LogInformation("Login successful for user: {Email}, session: {SessionId}", request.Email, sessionId);

        return new LoginResponseDto(sessionId, csrfToken);
    }

    public async Task LogoutAsync(LogoutRequestDto request, CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepository.GetAsync(request.SessionId, cancellationToken);
        if (session != null)
        {
            if (!string.IsNullOrEmpty(session.RefreshToken))
            {
                await _authService.LogoutAsync(session.RefreshToken, cancellationToken);
            }

            await _sessionRepository.DeleteAsync(request.SessionId, cancellationToken);
            _logger.LogInformation("Session {SessionId} logged out successfully", request.SessionId);
        }
    }

    public async Task<SessionInfoDto?> GetSessionInfoAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepository.GetAsync(sessionId, cancellationToken);
        if (session == null)
            return null;

        return new SessionInfoDto(
            session.SessionId,
            session.UserId ?? string.Empty,
            session.UserRole ?? string.Empty,
            session.Scopes,
            session.ExpiresAt,
            _validationService.IsSessionValid(session)
        );
    }

    public async Task<bool> RefreshTokenAsync(RefreshTokenRequestDto request, CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepository.GetAsync(request.SessionId, cancellationToken);
        if (session == null)
        {
            _logger.LogWarning("Session not found for refresh: {SessionId}", request.SessionId);
            return false;
        }

        if (!session.CanRefresh())
        {
            _logger.LogWarning("Session cannot be refreshed: {SessionId}", request.SessionId);
            return false;
        }

        // Pass sessionId to auth service to maintain sync
        var tokenInfo = await _authService.RefreshTokenAsync(
            session.RefreshToken,
            request.SessionId,
            cancellationToken);

        session.UpdateAccessToken(tokenInfo.AccessToken, tokenInfo.ExpiresAt);

        if (!string.IsNullOrEmpty(tokenInfo.RefreshToken))
        {
            session.SetTokens(tokenInfo.AccessToken, tokenInfo.RefreshToken, tokenInfo.ExpiresAt, tokenInfo.RefreshTokenExpiresAt);
        }

        await _sessionRepository.SaveAsync(session, cancellationToken);

        _logger.LogInformation("Token refreshed for session: {SessionId}", request.SessionId);
        return true;
    }

    public async Task<string> CreateSessionAsync(CancellationToken cancellationToken = default)
    {
        var sessionId = GenerateSessionId();
        var csrfToken = GenerateCsrfToken();
        
        var session = Session.Create(sessionId, csrfToken);
        await _sessionRepository.SaveAsync(session, cancellationToken);

        _logger.LogInformation("Created new session: {SessionId}", sessionId);
        return sessionId;
    }

    public async Task InvalidateSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepository.GetAsync(sessionId, cancellationToken);
        if (session != null)
        {
            session.Invalidate();
            await _sessionRepository.SaveAsync(session, cancellationToken);
            _logger.LogInformation("Session invalidated: {SessionId}", sessionId);
        }
    }

    public async Task<Session?> GetFullSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepository.GetAsync(sessionId, cancellationToken);
        if (session == null)
        {
            _logger.LogWarning("Session not found: {SessionId}", sessionId);
            return null;
        }

        _validationService.ValidateSessionOrThrow(session, sessionId);
        return session;
    }

    public async Task UpdateSessionAsync(Session session, CancellationToken cancellationToken = default)
    {
        await _sessionRepository.SaveAsync(session, cancellationToken);
        _logger.LogDebug("Session updated: {SessionId}", session.SessionId);
    }

    private static string GenerateSessionId()
    {
        return Guid.NewGuid().ToString("N");
    }

    private static string GenerateCsrfToken()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[32];
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }
}