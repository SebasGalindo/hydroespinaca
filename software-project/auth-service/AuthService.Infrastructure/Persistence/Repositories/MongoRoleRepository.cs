using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using AuthService.Infrastructure.Persistence.Mappers;
using AuthService.Infrastructure.Persistence.Schemas;
using HydroEspinaca.Shared.Mongo;
using MongoDB.Driver;

namespace AuthService.Infrastructure.Persistence.Repositories;

/// <summary>
/// MongoDB repository implementation for role entities.
/// </summary>
public class MongoRoleRepository : IRoleRepository
{
    private readonly BaseMongoRepository<Role, RoleDocument> _baseRepo;

    public MongoRoleRepository(IMongoDatabase db)
    {
        _baseRepo = new BaseMongoRepository<Role, RoleDocument>(
            db,
            "roles",
            new RoleMapper()
        );
    }

    public Task<Role?> FindByIdAsync(string id)
        => _baseRepo.GetByIdAsync(id);

    public async Task<Role?> FindByCodeAsync(string code)
    {
        var filter = Builders<RoleDocument>.Filter.Eq(x => x.Code, code);
        return await _baseRepo.FindOneAsync(filter);
    }

    public Task<List<Role>> GetAllAsync()
        => _baseRepo.GetAllAsync();

    public Task CreateAsync(Role role)
        => _baseRepo.CreateAsync(role);

    public Task UpdateAsync(Role role)
        => _baseRepo.UpdateAsync(role);

    public Task DeleteAsync(string id)
        => _baseRepo.DeleteAsync(id);

    public async Task<bool> ExistsAsync(string id)
    {
        var filter = Builders<RoleDocument>.Filter.Eq(x => x.Id, id);
        return await _baseRepo.ExistsAsync(filter);
    }

    public async Task<bool> ExistsByCodeAsync(string code)
    {
        var filter = Builders<RoleDocument>.Filter.Eq(x => x.Code, code);
        return await _baseRepo.ExistsAsync(filter);
    }

    public Task<Role?> FindByNameAsync(string name)
    {
        var filter = Builders<RoleDocument>.Filter.Eq(x => x.Name, name);
        return _baseRepo.FindOneAsync(filter);
    }
}