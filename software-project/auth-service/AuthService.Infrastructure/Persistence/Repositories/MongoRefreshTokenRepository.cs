using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using AuthService.Infrastructure.Persistence.Mappers;
using AuthService.Infrastructure.Persistence.Schemas;
using HydroEspinaca.Shared.Mongo;
using MongoDB.Driver;

namespace AuthService.Infrastructure.Persistence.Repositories;
public class MongoRefreshTokenRepository : IRefreshTokenRepository
{
    private readonly BaseMongoRepository<RefreshToken, RefreshTokenDocument> _baseRepo;

    public MongoRefreshTokenRepository(IMongoDatabase db)
    {
        _baseRepo = new BaseMongoRepository<RefreshToken, RefreshTokenDocument>(
            db,
            "refresh_tokens",
            new RefreshTokenMapper()
        );
    }

    public Task AddAsync(RefreshToken token)
        => _baseRepo.CreateAsync(token);

    public async Task<RefreshToken?> FindAsync(string token)
    {
        var filter = Builders<RefreshTokenDocument>.Filter.Eq(x => x.Token, token);
        return await _baseRepo.FindOneAsync(filter);
    }

    public Task UpdateAsync(RefreshToken token)
        => _baseRepo.UpdateAsync(token);
}