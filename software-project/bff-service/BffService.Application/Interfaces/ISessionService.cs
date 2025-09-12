using BffService.Application.DTOs;
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
}