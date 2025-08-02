using HydroEspinaca.Shared.Abstractions;
using HydroEspinaca.Shared.Mongo.Interfaces;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Linq.Expressions;

namespace HydroEspinaca.Shared.Mongo;

public class BaseMongoRepository<TEntity, TDocument> where TEntity : IIdentifiableMutable
    where TDocument : IIdentifiableMutable
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
        var objectId = ObjectId.Parse(id);
        var filter = BuildIdFilter<TDocument>(id);
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
        var inserted = _mapper.ToEntity(doc);
        entity.SetId(inserted.Id);
    }
    private static FilterDefinition<TDocument> BuildIdFilter<TDocument>(string id)
    {
        if (ObjectId.TryParse(id, out var objectId))
        {
            return Builders<TDocument>.Filter.Eq("_id", objectId);
        }

        return Builders<TDocument>.Filter.Eq("_id", id);
    }

    public async Task UpdateAsync(TEntity entity)
    {
        var doc = _mapper.ToDocument(entity);
        var filter = BuildIdFilter<TDocument>(doc.Id);

        await _collection.ReplaceOneAsync(filter, doc);
    }


    public async Task DeleteAsync(string id)
    {
        var filter = Builders<TDocument>.Filter.Eq("_id", id);
        await _collection.DeleteOneAsync(filter);
    }

    public async Task<List<TEntity>> FindManyAsync(
     FilterDefinition<TDocument> filter,
     SortDefinition<TDocument>? sort = null)
    {
        var findFluent = _collection.Find(filter);
        if (sort != null)
            findFluent = findFluent.Sort(sort);

        var docs = await findFluent.ToListAsync();
        return docs.Select(_mapper.ToEntity).ToList();
    }

    public async Task<TEntity?> FindLastOneAsync(
        FilterDefinition<TDocument> filter,
        SortDefinition<TDocument>? sort = null)
    {
        var findFluent = _collection.Find(filter);

        if (sort != null)
            findFluent = findFluent.Sort(sort);

        var doc = await findFluent.FirstOrDefaultAsync();
        return doc is null ? default : _mapper.ToEntity(doc);
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

    public async Task<DeleteResult> DeleteManyAsync(FilterDefinition<TDocument> filter)
    {
        return await _collection.DeleteManyAsync(filter);
    }

    public async Task ReplaceAsync(FilterDefinition<TDocument> filter, TEntity entity)
    {
        var doc = _mapper.ToDocument(entity);
        await _collection.ReplaceOneAsync(filter, doc);
    }

    public async Task<bool> UpdateFieldAsync<TField>(string id, Expression<Func<TDocument, TField>> field, TField value)
    {
        FilterDefinition<TDocument> filter = Builders<TDocument>.Filter.Eq("_id", id);
        UpdateDefinition<TDocument> update = Builders<TDocument>.Update.Set(field, value);
        
        var result = await _collection.UpdateOneAsync(filter, update);
        return result.MatchedCount > 0 && result.ModifiedCount > 0;
    }


    public async Task<List<string>> GetNonExistingIdsAsync(IEnumerable<string> ids)
    {
        if (ids == null) return new();

        var objectIdList = new List<ObjectId>();
        var stringIdList = new List<string>();

        foreach (var id in ids)
        {
            if (ObjectId.TryParse(id, out var objectId))
                objectIdList.Add(objectId);
            else
                stringIdList.Add(id);
        }

        FilterDefinition<TDocument> filter;

        if (objectIdList.Any() && stringIdList.Any())
        {
            filter = Builders<TDocument>.Filter.Or(
                Builders<TDocument>.Filter.In("_id", objectIdList),
                Builders<TDocument>.Filter.In("_id", stringIdList)
            );
        }
        else if (objectIdList.Any())
        {
            filter = Builders<TDocument>.Filter.In("_id", objectIdList);
        }
        else
        {
            filter = Builders<TDocument>.Filter.In("_id", stringIdList);
        }

        var existing = await _collection
            .Find(filter)
            .Project(d => d.Id.ToString())
            .ToListAsync();

        return ids.Except(existing).ToList();
    }

}
