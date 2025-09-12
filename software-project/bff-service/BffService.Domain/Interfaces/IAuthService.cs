using BffService.Domain.ValueObjects;

namespace BffService.Domain.Interfaces;

public interface IAuthService
{
    Task<AuthenticationResult> LoginAsync(string email, string password, CancellationToken cancellationToken = default);
    Task<TokenInfo> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<bool> ValidateTokenAsync(string accessToken, CancellationToken cancellationToken = default);
    Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default);
}