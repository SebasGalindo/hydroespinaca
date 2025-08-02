using AuthService.Domain.ValueObjects;

namespace AuthService.Domain.Interfaces;
public interface IRefreshTokenService
{
    Task<RefreshTokenResult> ValidateAndRotateAsync(string refreshToken);
}
