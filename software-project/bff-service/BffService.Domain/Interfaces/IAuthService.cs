using BffService.Domain.ValueObjects;

namespace BffService.Domain.Interfaces;

/// <summary>
/// Contract for the authentication service that communicates with the auth microservice.
/// </summary>
public interface IAuthService
{
    Task<AuthenticationResult> LoginAsync(
        string email,
        string password,
        string? clientId = null,
        string? sessionId = null,
        string? csrfToken = null,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default);

    Task<TokenInfo> RefreshTokenAsync(
        string refreshToken,
        string? sessionId = null,
        CancellationToken cancellationToken = default);

    Task<bool> ValidateTokenAsync(string accessToken, CancellationToken cancellationToken = default);
    Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default);
}