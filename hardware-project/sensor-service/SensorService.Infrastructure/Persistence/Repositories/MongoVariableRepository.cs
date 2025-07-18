using SensorService.Domain.Exceptions;
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
        try
        {
            var doc = await _collection.Find(x => x.Id == id).FirstOrDefaultAsync();
            return doc is null ? null : VariableMapper.ToEntity(doc);
        }
        catch (Exception ex)
        {
            throw new DatabaseOperationException("Error retrieving variable by ID", ex);
        }
    }

    public async Task<List<Variable>> GetAllAsync()
    {
        try
        {
            var docs = await _collection.Find(_ => true).ToListAsync();
            return docs.Select(VariableMapper.ToEntity).ToList();
        }
        catch (Exception ex)
        {
            throw new DatabaseOperationException("Error retrieving all variables", ex);
        }
    }

    public async Task CreateAsync(Variable variable)
    {
        try
        {
            await _collection.InsertOneAsync(VariableMapper.ToDocument(variable));
        }
        catch (Exception ex)
        {
            throw new DatabaseOperationException("Error creating variable", ex);
        }
    }

    public async Task UpdateAsync(Variable variable)
    {
        try
        {
            await _collection.ReplaceOneAsync(x => x.Id == variable.Id, VariableMapper.ToDocument(variable));
        }
        catch (Exception ex)
        {
            throw new DatabaseOperationException("Error updating variable", ex);
        }
    }

    public async Task DeleteAsync(string id)
    {
        try
        {
            await _collection.DeleteOneAsync(x => x.Id == id);
        }
        catch (Exception ex)
        {
            throw new DatabaseOperationException("Error deleting variable", ex);
        }
    }
    public async Task<int> CountByIdsAsync(IEnumerable<string> ids)
    {
        return (int)await _collection.CountDocumentsAsync(x => ids.Contains(x.Id));
    }
}
