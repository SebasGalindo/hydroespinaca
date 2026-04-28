using BffService.Domain.Entities;
using HydroEspinaca.Shared.DTOs.Authentication;

namespace BffService.Application.Interfaces;

public interface ISessionService
{
    Task<LoginResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);
    Task LogoutAsync(LogoutRequestDto request, CancellationToken cancellationToken = default);
    Task<SessionInfoDto?> GetSessionInfoAsync(string sessionId, CancellationToken cancellationToken = default);
    Task<bool> RefreshTokenAsync(RefreshTokenRequestDto request, CancellationToken cancellationToken = default);
    Task<string> CreateSessionAsync(CancellationToken cancellationToken = default);
    Task InvalidateSessionAsync(string sessionId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the full session entity with all token information for proxy requests
    /// </summary>
    Task<Session?> GetFullSessionAsync(string sessionId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates a session entity (used when tokens are refreshed)
    /// </summary>
    Task UpdateSessionAsync(Session session, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a session from the cache
    /// </summary>
    Task DeleteSessionAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Accepts terms and conditions for the user (calls auth-service to persist the acceptance).
    /// </summary>
    Task AcceptTermsAsync(string userId, string accessToken, CancellationToken cancellationToken = default);
}