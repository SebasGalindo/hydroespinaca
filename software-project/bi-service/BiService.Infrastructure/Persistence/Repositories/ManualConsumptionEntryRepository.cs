using BiService.Domain.Entities;
using BiService.Domain.Enums;
using BiService.Domain.Interfaces;
using BiService.Infrastructure.Persistence.Models;
using HydroEspinaca.Shared.Mongo.Interfaces;
using MongoDB.Driver;

namespace BiService.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositorio MongoDB para los registros de consumo manual de recursos.
/// </summary>
public class ManualConsumptionEntryRepository : IManualConsumptionEntryRepository
{
    private const string CollectionName = "bi_manual_consumption_entries";
    private readonly IMongoCollection<ManualConsumptionEntryDocument> _collection;
    private readonly IEntityMapper<ManualConsumptionEntry, ManualConsumptionEntryDocument> _mapper;

    public ManualConsumptionEntryRepository(
        IMongoDatabase database,
        IEntityMapper<ManualConsumptionEntry, ManualConsumptionEntryDocument> mapper)
    {
        _collection = database.GetCollection<ManualConsumptionEntryDocument>(CollectionName);
        _mapper = mapper;
    }

    /// <summary>
    /// Obtiene un registro de consumo manual por su identificador.
    /// </summary>
    /// <param name="id">Identificador del registro.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>El registro encontrado, o <c>null</c> si no existe.</returns>
    public async Task<ManualConsumptionEntry?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var filter = Builders<ManualConsumptionEntryDocument>.Filter.Eq(x => x.Id, id);
        var doc = await _collection.Find(filter).FirstOrDefaultAsync(ct);
        return doc is null ? null : _mapper.ToEntity(doc);
    }

    /// <summary>
    /// Crea un nuevo registro de consumo manual en la base de datos.
    /// </summary>
    /// <param name="entry">Entidad de consumo manual a persistir.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>La entidad creada con su identificador asignado.</returns>
    public async Task<ManualConsumptionEntry> CreateAsync(ManualConsumptionEntry entry, CancellationToken cancellationToken = default)
    {
        // Single-document insert: transaction is not required here.
        // We use cancellationToken explicitly to support request cancellation.
        var doc = _mapper.ToDocument(entry);
        await _collection.InsertOneAsync(doc, cancellationToken: cancellationToken);
        entry.SetId(doc.Id);
        return entry;
    }

    /// <summary>
    /// Obtiene los registros de consumo manual dentro de un rango de fechas, opcionalmente filtrados por tipo de consumo.
    /// </summary>
    /// <param name="from">Fecha de inicio del rango (inclusive).</param>
    /// <param name="to">Fecha de fin del rango (inclusive).</param>
    /// <param name="type">Tipo de consumo para filtrar. Puede ser <c>null</c> para obtener todos los tipos.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Lista de registros ordenados descendentemente por fecha.</returns>
    public async Task<IReadOnlyList<ManualConsumptionEntry>> GetByRangeAsync(
        DateTime from,
        DateTime to,
        ConsumptionType? type,
        CancellationToken cancellationToken = default)
    {
        var filters = new List<FilterDefinition<ManualConsumptionEntryDocument>>
        {
            Builders<ManualConsumptionEntryDocument>.Filter.Lte(x => x.DateFrom, to),
            Builders<ManualConsumptionEntryDocument>.Filter.Gte(x => x.DateTo, from)
        };

        if (type.HasValue)
        {
            filters.Add(Builders<ManualConsumptionEntryDocument>.Filter.Eq(x => x.Type, type.Value));
        }

        var filter = Builders<ManualConsumptionEntryDocument>.Filter.And(filters);
        var docs = await _collection
            .Find(filter)
            .SortByDescending(x => x.DateFrom)
            .ToListAsync(cancellationToken);

        return docs.Select(_mapper.ToEntity).ToList();
    }

    /// <summary>
    /// Elimina un registro de consumo manual por su identificador.
    /// </summary>
    /// <param name="id">Identificador del registro a eliminar.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns><c>true</c> si se eliminó el registro; <c>false</c> si no se encontró.</returns>
    public async Task<bool> DeleteAsync(string id, CancellationToken ct = default)
    {
        var filter = Builders<ManualConsumptionEntryDocument>.Filter.Eq(x => x.Id, id);
        var result = await _collection.DeleteOneAsync(filter, ct);
        return result.DeletedCount > 0;
    }
}
