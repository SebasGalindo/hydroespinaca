using BiService.Domain.Entities;
using BiService.Domain.Interfaces;
using BiService.Infrastructure.Persistence.Models;
using HydroEspinaca.Shared.Mongo.Interfaces;
using MongoDB.Driver;

namespace BiService.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositorio MongoDB para los registros de producción de cultivos.
/// </summary>
public class ProductionRecordRepository : IProductionRecordRepository
{
    private const string CollectionName = "bi_production_records";
    private readonly IMongoCollection<ProductionRecordDocument> _collection;
    private readonly IEntityMapper<ProductionRecord, ProductionRecordDocument> _mapper;

    public ProductionRecordRepository(
        IMongoDatabase database,
        IEntityMapper<ProductionRecord, ProductionRecordDocument> mapper)
    {
        _collection = database.GetCollection<ProductionRecordDocument>(CollectionName);
        _mapper = mapper;
    }

    /// <summary>
    /// Obtiene un registro de producción por su identificador.
    /// </summary>
    /// <param name="id">Identificador del registro.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>El registro encontrado, o <c>null</c> si no existe.</returns>
    public async Task<ProductionRecord?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var filter = Builders<ProductionRecordDocument>.Filter.Eq(x => x.Id, id);
        var doc = await _collection.Find(filter).FirstOrDefaultAsync(ct);
        return doc is null ? null : _mapper.ToEntity(doc);
    }

    /// <summary>
    /// Obtiene todos los registros de producción ordenados descendentemente por fecha de cosecha.
    /// </summary>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Lista de todos los registros de producción.</returns>
    public async Task<IReadOnlyList<ProductionRecord>> GetAllAsync(CancellationToken ct = default)
    {
        var sort = Builders<ProductionRecordDocument>.Sort.Descending(x => x.HarvestDate);
        var docs = await _collection.Find(Builders<ProductionRecordDocument>.Filter.Empty)
            .Sort(sort).ToListAsync(ct);
        return docs.Select(_mapper.ToEntity).ToList();
    }

    /// <summary>
    /// Crea un nuevo registro de producción en la base de datos.
    /// </summary>
    /// <param name="record">Entidad de producción a persistir.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>La entidad creada con su identificador asignado.</returns>
    public async Task<ProductionRecord> CreateAsync(ProductionRecord record, CancellationToken ct = default)
    {
        var doc = _mapper.ToDocument(record);
        await _collection.InsertOneAsync(doc, cancellationToken: ct);
        record.SetId(doc.Id);
        return record;
    }

    /// <summary>
    /// Elimina un registro de producción por su identificador.
    /// </summary>
    /// <param name="id">Identificador del registro a eliminar.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns><c>true</c> si se eliminó el registro; <c>false</c> si no se encontró.</returns>
    public async Task<bool> DeleteAsync(string id, CancellationToken ct = default)
    {
        var filter = Builders<ProductionRecordDocument>.Filter.Eq(x => x.Id, id);
        var result = await _collection.DeleteOneAsync(filter, ct);
        return result.DeletedCount > 0;
    }
}
