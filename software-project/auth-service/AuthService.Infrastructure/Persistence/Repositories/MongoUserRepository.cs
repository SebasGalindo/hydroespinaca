using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using AuthService.Infrastructure.Persistence.Mappers;
using AuthService.Infrastructure.Persistence.Schemas;
using HydroEspinaca.Shared.Mongo;
using MongoDB.Driver;

namespace AuthService.Infrastructure.Persistence.Repositories;

public class MongoUserRepository : IUserRepository
{
    private readonly BaseMongoRepository<User, UserDocument> _baseRepo;

    public MongoUserRepository(IMongoDatabase db)
    {
        _baseRepo = new BaseMongoRepository<User, UserDocument>(
            db,
            "users",
            new UserMapper()
        );
    }

    public Task<User?> FindByEmailAsync(string email)
    {
        var filter = Builders<UserDocument>.Filter.Eq(x => x.Email, email);
        return _baseRepo.FindOneAsync(filter);
    }

    public Task<User?> FindByIdAsync(Guid id)
        => _baseRepo.GetByIdAsync(id.ToString());

    public async Task<User?> FindByIdAsync(string id)
    {
        try
        {
            return await _baseRepo.GetByIdAsync(id);
        }
        catch (FormatException)
        {

            var filter = Builders<UserDocument>.Filter.Eq(x => x.Id, id);
            return await _baseRepo.FindOneAsync(filter);
        }
    }

    public Task<List<User>> GetAllAsync()
        => _baseRepo.GetAllAsync();

    public Task CreateAsync(User user)
        => _baseRepo.CreateAsync(user);

    public Task UpdateAsync(User user)
        => _baseRepo.UpdateAsync(user);

    public Task DeleteAsync(string id)
        => _baseRepo.DeleteAsync(id);
}