using AuthService.Domain.Entities;

namespace AuthService.Domain.Interfaces;
public interface IUserRepository
{
    Task<User?> FindByEmailAsync(string email);
    Task<User?> FindByIdAsync(Guid id);
    Task<User?> FindByIdAsync(string id);
    Task<List<User>> GetAllAsync();
    Task CreateAsync(User user);
    Task UpdateAsync(User variable);
    Task DeleteAsync(string id);
}