using AuthService.Domain.Entities;

namespace AuthService.Domain.Interfaces;

public interface IPermissionRepository
{
    Task<Permission?> FindByIdAsync(string id);
    Task<Permission?> FindByCodeAsync(string code);
    Task<List<Permission>> GetAllAsync();
    Task CreateAsync(Permission permission);
    Task UpdateAsync(Permission permission);
    Task DeleteAsync(string id);
    Task<bool> ExistsAsync(string id);
    Task<bool> ExistsByCodeAsync(string code);
    Task<List<Permission>> FindByIdsAsync(IEnumerable<string> ids);
    Task<List<Permission>> FindByCodesAsync(IEnumerable<string> codes);
    Task<List<string>> GetNonExistingIdsAsync(IEnumerable<string> ids);
    Task<List<string>> GetNonExistingCodesAsync(IEnumerable<string> codes);
    Task<List<GroupedPermissionsDto>> GetGroupedAsync();
}