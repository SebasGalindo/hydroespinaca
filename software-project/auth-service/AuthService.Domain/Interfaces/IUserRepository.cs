using AuthService.Domain.Entities;

namespace AuthService.Domain.Interfaces;
public interface IUserRepository
{
    Task<User?> FindByEmailAsync(string email);
    Task<User?> FindByIdAsync(Guid id);
    Task CreateAsync(User user);
    Task UpdateAsync(User variable);
    Task DeleteAsync(string id);
}