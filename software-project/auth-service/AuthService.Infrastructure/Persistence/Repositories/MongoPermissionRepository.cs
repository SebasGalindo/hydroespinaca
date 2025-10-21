using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using AuthService.Infrastructure.Persistence.Mappers;
using AuthService.Infrastructure.Persistence.Schemas;
using HydroEspinaca.Shared.Mongo;
using MongoDB.Driver;

namespace AuthService.Infrastructure.Persistence.Repositories;

public class MongoPermissionRepository : IPermissionRepository
{
    private readonly BaseMongoRepository<Permission, PermissionDocument> _baseRepo;

    public MongoPermissionRepository(IMongoDatabase db)
    {
        _baseRepo = new BaseMongoRepository<Permission, PermissionDocument>(
            db,
            "permissions",
            new PermissionMapper()
        );
    }

    public Task<Permission?> FindByIdAsync(string id)
        => _baseRepo.GetByIdAsync(id);

    public async Task<Permission?> FindByCodeAsync(string code)
    {
        var filter = Builders<PermissionDocument>.Filter.Eq(x => x.Code, code);
        return await _baseRepo.FindOneAsync(filter);
    }

    public Task<List<Permission>> GetAllAsync()
        => _baseRepo.GetAllAsync();

    public Task CreateAsync(Permission permission)
        => _baseRepo.CreateAsync(permission);

    public Task UpdateAsync(Permission permission)
        => _baseRepo.UpdateAsync(permission);

    public Task DeleteAsync(string id)
        => _baseRepo.DeleteAsync(id);

    public async Task<bool> ExistsAsync(string id)
    {
        var filter = Builders<PermissionDocument>.Filter.Eq(x => x.Id, id);
        return await _baseRepo.ExistsAsync(filter);
    }

    public async Task<bool> ExistsByCodeAsync(string code)
    {
        var filter = Builders<PermissionDocument>.Filter.Eq(x => x.Code, code);
        return await _baseRepo.ExistsAsync(filter);
    }

    public async Task<List<Permission>> FindByIdsAsync(IEnumerable<string> ids)
    {
        var filter = Builders<PermissionDocument>.Filter.In(x => x.Id, ids);
        return await _baseRepo.FindManyAsync(filter);
    }

    public async Task<List<Permission>> FindByCodesAsync(IEnumerable<string> codes)
    {
        var filter = Builders<PermissionDocument>.Filter.In(x => x.Code, codes);
        return await _baseRepo.FindManyAsync(filter);
    }

    public Task<List<string>> GetNonExistingIdsAsync(IEnumerable<string> ids)
        => _baseRepo.GetNonExistingIdsAsync(ids);

    public async Task<List<string>> GetNonExistingCodesAsync(IEnumerable<string> codes)
    {
        var filter = Builders<PermissionDocument>.Filter.In(x => x.Code, codes);
        var existingPermissions = await _baseRepo.FindManyAsync(filter);
        var existingCodes = existingPermissions.Select(p => p.Code).ToHashSet();
        return codes.Where(code => !existingCodes.Contains(code)).ToList();
    }

    public async Task<List<GroupedPermissions>> GetGroupedAsync()
    {
        var all = await _baseRepo.GetAllAsync();
        var groups = all
            .GroupBy(p => p.Code.Split(':')[0])
            .Select(g => new GroupedPermissions
            {
                Category = g.Key,
                Permissions = g.OrderBy(x => x.Code).ToList()
            })
            .OrderBy(g => g.Category)
            .ToList();

        return groups;
    }
}