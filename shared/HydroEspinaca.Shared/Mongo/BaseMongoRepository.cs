using HydroEspinaca.Shared.Mongo.Interfaces;
using MongoDB.Driver;

namespace HydroEspinaca.Shared.Mongo;

public class BaseMongoRepository<TEntity, TDocument>
{
    private readonly IMongoCollection<TDocument> _collection;
    private readonly IEntityMapper<TEntity, TDocument> _mapper;

    public BaseMongoRepository(IMongoDatabase db, string collectionName, IEntityMapper<TEntity, TDocument> mapper)
    {
        _collection = db.GetCollection<TDocument>(collectionName);
        _mapper = mapper;
    }

    public async Task<TEntity?> GetByIdAsync(string id)
    {
        var filter = Builders<TDocument>.Filter.Eq("_id", id);
        var doc = await _collection.Find(filter).FirstOrDefaultAsync();
        return doc is null ? default : _mapper.ToEntity(doc);
    }

    public async Task<List<TEntity>> GetAllAsync()
    {
        var docs = await _collection.Find(_ => true).ToListAsync();
        return docs.Select(_mapper.ToEntity).ToList();
    }

    public async Task CreateAsync(TEntity entity)
    {
        var doc = _mapper.ToDocument(entity);
        await _collection.InsertOneAsync(doc);
    }

    public async Task UpdateAsync(TEntity entity)
    {
        var doc = _mapper.ToDocument(entity);
        var id = doc?.GetType().GetProperty("Id")?.GetValue(doc)?.ToString();

        if (id == null)
            throw new InvalidOperationException("Document must have an Id property");

        var filter = Builders<TDocument>.Filter.Eq("_id", id);
        await _collection.ReplaceOneAsync(filter, doc);
    }

    public async Task DeleteAsync(string id)
    {
        var filter = Builders<TDocument>.Filter.Eq("_id", id);
        await _collection.DeleteOneAsync(filter);
    }

    public async Task<List<TEntity>> FindManyAsync(FilterDefinition<TDocument> filter)
    {
        var docs = await _collection.Find(filter).ToListAsync();
        return docs.Select(_mapper.ToEntity).ToList();
    }

    public async Task<TEntity?> FindOneAsync(FilterDefinition<TDocument> filter)
    {
        var doc = await _collection.Find(filter).FirstOrDefaultAsync();
        return doc is null ? default : _mapper.ToEntity(doc);
    }

    public async Task<bool> ExistsAsync(FilterDefinition<TDocument> filter)
    {
        return await _collection.Find(filter).Limit(1).AnyAsync();
    }

    public async Task<long> CountAsync(FilterDefinition<TDocument> filter)
    {
        return await _collection.CountDocumentsAsync(filter);
    }

    public async Task DeleteManyAsync(FilterDefinition<TDocument> filter)
    {
        await _collection.DeleteManyAsync(filter);
    }

    public async Task ReplaceAsync(FilterDefinition<TDocument> filter, TEntity entity)
    {
        var doc = _mapper.ToDocument(entity);
        await _collection.ReplaceOneAsync(filter, doc);
    }
}
