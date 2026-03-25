using AuthService.Domain.ValueObjects;

namespace AuthService.Domain.Interfaces;
/// <summary>
/// Contract for refresh token generation, validation, and rotation.
/// </summary>
public interface IRefreshTokenService
{
    Task<RefreshTokenResult> ValidateAndRotateAsync(string refreshToken);
}
