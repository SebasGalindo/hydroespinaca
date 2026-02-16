using AuthService.Application.Exceptions;
using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace AuthService.Infrastructure.Services;

/// <summary>
/// Infrastructure service for managing user login sessions.
/// </summary>
public class UserSessionService : IUserSessionService
{
    private readonly IUserSessionRepository _sessionRepository;

    // Thread-safe lock dictionary to prevent concurrent refresh operations on the same refresh token
    // Key: refreshToken, Value: SemaphoreSlim for synchronization
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _refreshLocks = new();

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
        // Get or create a semaphore for this refresh token to prevent concurrent refresh operations
        var semaphore = _refreshLocks.GetOrAdd(refreshToken, _ => new SemaphoreSlim(1, 1));

        // Wait for exclusive access to refresh this token
        await semaphore.WaitAsync();
        try
        {
            // Double-check: find session again after acquiring lock
            // Another thread might have already refreshed it while we were waiting
            var session = await _sessionRepository.FindByRefreshTokenAsync(refreshToken);
            if (session == null)
            {
                // Token was already refreshed by another thread or is invalid
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
        finally
        {
            semaphore.Release();

            // Clean up the semaphore from the dictionary to prevent memory leaks
            // Only remove if no other threads are waiting
            if (semaphore.CurrentCount == 1)
            {
                _refreshLocks.TryRemove(refreshToken, out _);
            }
        }
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
