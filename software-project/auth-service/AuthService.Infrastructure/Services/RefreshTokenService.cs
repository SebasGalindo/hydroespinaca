using AuthService.Application.Exceptions;
using AuthService.Domain.Interfaces;
using AuthService.Domain.ValueObjects;

namespace AuthService.Infrastructure.Services;
public class RefreshTokenService : IRefreshTokenService
{
    private readonly IRefreshTokenRepository _refreshRepo;
    private readonly IUserRepository _userRepo;

    public RefreshTokenService(
        IRefreshTokenRepository refreshRepo,
        IUserRepository userRepo)
    {
        _refreshRepo = refreshRepo;
        _userRepo = userRepo;
    }

    public async Task<RefreshTokenResult> ValidateAndRotateAsync(string refreshToken)
    {
        var stored = await _refreshRepo.FindAsync(refreshToken)
                     ?? throw new InvalidRefreshTokenException();

        if (!stored.IsActive)
            throw new InvalidRefreshTokenException();

        stored.Revoke();
        await _refreshRepo.UpdateAsync(stored);

        var user = await _userRepo.FindByIdAsync(stored.UserId)
                   ?? throw new InvalidRefreshTokenException();

        var clientId = stored.ClientId;
        return new RefreshTokenResult(
            Guid.Parse(user.Id),
            user.Email.Value,
            user.Role.ToString(),
            clientId
        );
    }
}