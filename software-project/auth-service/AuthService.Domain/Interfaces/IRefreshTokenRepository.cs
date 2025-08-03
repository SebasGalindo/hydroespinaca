using AuthService.Domain.Entities;

namespace AuthService.Domain.Interfaces;
public interface IRefreshTokenRepository
{
    Task AddAsync(RefreshToken token);
    Task<RefreshToken?> FindAsync(string token);
    Task UpdateAsync(RefreshToken token);
}

