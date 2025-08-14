using AuthService.Domain.Entities;

namespace AuthService.Domain.Interfaces;

public interface IRoleRepository
{
    Task<Role?> FindByIdAsync(string id);
    Task<Role?> FindByCodeAsync(string code);
    Task<List<Role>> GetAllAsync();
    Task CreateAsync(Role role);
    Task UpdateAsync(Role role);
    Task DeleteAsync(string id);
    Task<bool> ExistsAsync(string id);
    Task<bool> ExistsByCodeAsync(string code);
    Task<Role?> FindByNameAsync(string name);
}