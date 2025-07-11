using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using SensorService.Domain.Interfaces;
using SensorService.Infrastructure.Persistence.Mappers;
using SensorService.Infrastructure.Persistence.Models;

namespace SensorService.Infrastructure.Persistence.Repositories;
public class MongoVariableRepository : IVariableRepository
{
    private readonly IMongoCollection<VariableDocument> _collection;

    public MongoVariableRepository(IConfiguration config)
    {
        var client = new MongoClient(config["Mongo:ConnectionString"]);
        var db = client.GetDatabase(config["Mongo:Database"]);
        _collection = db.GetCollection<VariableDocument>("variables");
    }

    public async Task<Variable?> GetByIdAsync(string id)
    {
        var doc = await _collection.Find(x => x.Id == id).FirstOrDefaultAsync();
        return doc is null ? null : VariableMapper.ToEntity(doc);
    }

    public async Task<List<Variable>> GetAllAsync()
    {
        var docs = await _collection.Find(_ => true).ToListAsync();
        return docs.Select(VariableMapper.ToEntity).ToList();
    }

    public async Task CreateAsync(Variable variable)
    {
        await _collection.InsertOneAsync(VariableMapper.ToDocument(variable));
    }

    public async Task UpdateAsync(Variable variable)
    {
        await _collection.ReplaceOneAsync(x => x.Id == variable.Id, VariableMapper.ToDocument(variable));
    }

    public async Task DeleteAsync(string id)
    {
        await _collection.DeleteOneAsync(x => x.Id == id);
    }
}
