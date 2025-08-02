using AuthService.Domain.Aggregates;

namespace AuthService.Domain.Interfaces;
public interface IRefreshTokenRepository
{
    Task AddAsync(RefreshToken token);
    Task<RefreshToken?> FindAsync(string token);
    Task UpdateAsync(RefreshToken token);
}

