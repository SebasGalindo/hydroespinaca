using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using AuthService.Infrastructure.Persistence.Mappers;
using AuthService.Infrastructure.Persistence.Schemas;
using HydroEspinaca.Shared.Mongo;
using MongoDB.Driver;

namespace AuthService.Infrastructure.Persistence.Repositories;
/// <summary>
/// MongoDB repository implementation for client application entities.
/// </summary>
public class MongoClientAppRepository : IClientAppRepository
{
    private readonly BaseMongoRepository<ClientApp, ClientAppDocument> _baseRepo;

    public MongoClientAppRepository(IMongoDatabase db)
    {
        _baseRepo = new BaseMongoRepository<ClientApp, ClientAppDocument>(
            db,
            "client_apps",
            new ClientAppMapper()
        );
    }

    public Task<ClientApp?> FindByClientIdAsync(string clientId)
    {
        var filter = Builders<ClientAppDocument>.Filter.Eq(x => x.ClientId, clientId);
        return _baseRepo.FindOneAsync(filter);
    }

    public Task AddAsync(ClientApp app)
    {
        return _baseRepo.CreateAsync(app);
    }
}