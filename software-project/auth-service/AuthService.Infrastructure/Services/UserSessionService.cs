using AuthService.Application.Exceptions;
using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace AuthService.Infrastructure.Services;

public class UserSessionService : IUserSessionService
{
    private readonly IUserSessionRepository _sessionRepository;

    public UserSessionService(IUserSessionRepository sessionRepository)
    {
        _sessionRepository = sessionRepository;
    }

    public async Task<UserSession> CreateSessionAsync(
        string userId,
        string clientId,
        string sessionId,
        string refreshToken,
        string accessToken,
        DateTime expiresAt,
        string? ipAddress = null,
        string? userAgent = null,
        string? csrfToken = null)
    {
        var accessTokenHash = ComputeHash(accessToken);

        var session = new UserSession(
            userId,
            clientId,
            sessionId,
            refreshToken,
            accessTokenHash,
            expiresAt,
            ipAddress,
            userAgent,
            csrfToken
        );

        await _sessionRepository.AddAsync(session);
        return session;
    }

    public async Task<UserSession> RefreshSessionAsync(
        string refreshToken,
        string newRefreshToken,
        string newAccessToken)
    {
        var session = await _sessionRepository.FindByRefreshTokenAsync(refreshToken);
        if (session == null)
        {
            throw new InvalidRefreshTokenException();
        }

        if (!session.IsActive)
        {
            throw new InvalidRefreshTokenException();
        }

        var newAccessTokenHash = ComputeHash(newAccessToken);
        session.UpdateTokens(newRefreshToken, newAccessTokenHash);

        await _sessionRepository.UpdateAsync(session);
        return session;
    }

    public async Task<IEnumerable<UserSession>> GetActiveUserSessionsAsync(string userId)
    {
        return await _sessionRepository.FindActiveByUserIdAsync(userId);
    }

    private static string ComputeHash(string input)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}
