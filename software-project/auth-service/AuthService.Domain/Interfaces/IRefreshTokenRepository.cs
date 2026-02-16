using AuthService.Domain.Entities;

namespace AuthService.Domain.Interfaces;
/// <summary>
/// Persistence contract for refresh token entities.
/// </summary>
public interface IRefreshTokenRepository
{
    Task AddAsync(RefreshToken token);
    Task<RefreshToken?> FindAsync(string token);
    Task UpdateAsync(RefreshToken token);
}

