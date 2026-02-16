using BiService.Domain.Entities;
using BiService.Domain.Interfaces;
using BiService.Infrastructure.Persistence.Models;
using HydroEspinaca.Shared.Mongo.Interfaces;
using MongoDB.Driver;

namespace BiService.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositorio MongoDB para las versiones de configuración de costos. Gestiona la lógica de activación/desactivación de versiones en transacciones.
/// </summary>
public class CostConfigVersionRepository : ICostConfigVersionRepository
{
    private const string CollectionName = "bi_cost_config_versions";
    private readonly IMongoCollection<CostConfigVersionDocument> _collection;
    private readonly IEntityMapper<CostConfigVersion, CostConfigVersionDocument> _mapper;

    public CostConfigVersionRepository(
        IMongoDatabase database,
        IEntityMapper<CostConfigVersion, CostConfigVersionDocument> mapper)
    {
        _collection = database.GetCollection<CostConfigVersionDocument>(CollectionName);
        _mapper = mapper;
    }

    /// <summary>
    /// Obtiene la versión de configuración de costos activa más reciente.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>La versión activa actual, o <c>null</c> si no existe ninguna.</returns>
    public async Task<CostConfigVersion?> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        var filter = Builders<CostConfigVersionDocument>.Filter.Eq(x => x.IsActive, true);
        var sort = Builders<CostConfigVersionDocument>.Sort.Descending(x => x.EffectiveFrom);

        var docs = await _collection.Find(filter).Sort(sort).Limit(1).ToListAsync(cancellationToken);
        if (docs.Count == 0)
        {
            return null;
        }

        return _mapper.ToEntity(docs[0]);
    }

    /// <summary>
    /// Obtiene las versiones de configuración de costos filtradas por un rango de fechas opcional.
    /// </summary>
    /// <param name="from">Fecha de inicio del filtro (inclusive). Puede ser <c>null</c>.</param>
    /// <param name="to">Fecha de fin del filtro (inclusive). Puede ser <c>null</c>.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Lista de versiones ordenadas descendentemente por fecha de vigencia.</returns>
    public async Task<IReadOnlyList<CostConfigVersion>> GetVersionsAsync(DateTime? from, DateTime? to, CancellationToken cancellationToken = default)
    {
        var filters = new List<FilterDefinition<CostConfigVersionDocument>>();

        if (from.HasValue)
        {
            filters.Add(Builders<CostConfigVersionDocument>.Filter.Gte(x => x.EffectiveFrom, from.Value));
        }

        if (to.HasValue)
        {
            filters.Add(Builders<CostConfigVersionDocument>.Filter.Lte(x => x.EffectiveFrom, to.Value));
        }

        var filter = filters.Count > 0
            ? Builders<CostConfigVersionDocument>.Filter.And(filters)
            : Builders<CostConfigVersionDocument>.Filter.Empty;

        var sort = Builders<CostConfigVersionDocument>.Sort.Descending(x => x.EffectiveFrom);
        var docs = await _collection.Find(filter).Sort(sort).ToListAsync(cancellationToken);
        return docs.Select(_mapper.ToEntity).ToList();
    }

    /// <summary>
    /// Obtiene todas las versiones cuyo período de vigencia se superpone con el rango especificado.
    /// </summary>
    /// <param name="from">Inicio del rango de consulta.</param>
    /// <param name="to">Fin del rango de consulta.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Lista de versiones ordenadas ascendentemente por fecha de vigencia.</returns>
    public async Task<IReadOnlyList<CostConfigVersion>> GetVersionsForRangeAsync(
        DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        // Find all versions whose effective period overlaps with [from, to]:
        // effectiveFrom <= to AND (effectiveTo >= from OR effectiveTo is null)
        var filter = Builders<CostConfigVersionDocument>.Filter.And(
            Builders<CostConfigVersionDocument>.Filter.Lte(x => x.EffectiveFrom, to),
            Builders<CostConfigVersionDocument>.Filter.Or(
                Builders<CostConfigVersionDocument>.Filter.Gte(x => x.EffectiveTo, from),
                Builders<CostConfigVersionDocument>.Filter.Eq(x => x.EffectiveTo, null)
            )
        );

        var sort = Builders<CostConfigVersionDocument>.Sort.Ascending(x => x.EffectiveFrom);
        var docs = await _collection.Find(filter).Sort(sort).ToListAsync(cancellationToken);
        return docs.Select(_mapper.ToEntity).ToList();
    }

    /// <summary>
    /// Crea una nueva versión de configuración de costos dentro de una transacción. Desactiva la versión activa anterior y establece la nueva como activa.
    /// </summary>
    /// <param name="version">Nueva versión de configuración a crear.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>La versión creada con su identificador asignado.</returns>
    public async Task<CostConfigVersion> CreateVersionAsync(CostConfigVersion version, CancellationToken cancellationToken = default)
    {
        using var session = await _collection.Database.Client.StartSessionAsync(cancellationToken: cancellationToken);
        session.StartTransaction();

        try
        {
            var currentFilter = Builders<CostConfigVersionDocument>.Filter.Eq(x => x.IsActive, true);
            var current = await _collection
                .Find(session, currentFilter)
                .SortByDescending(x => x.EffectiveFrom)
                .FirstOrDefaultAsync(cancellationToken);

            if (current is not null)
            {
                var update = Builders<CostConfigVersionDocument>.Update
                    .Set(x => x.IsActive, false)
                    .Set(x => x.EffectiveTo, version.EffectiveFrom);

                await _collection.UpdateOneAsync(
                    session,
                    Builders<CostConfigVersionDocument>.Filter.Eq(x => x.Id, current.Id),
                    update,
                    cancellationToken: cancellationToken);
            }

            var newDoc = _mapper.ToDocument(version);
            await _collection.InsertOneAsync(session, newDoc, cancellationToken: cancellationToken);
            version.SetId(newDoc.Id);

            await session.CommitTransactionAsync(cancellationToken);
            return version;
        }
        catch
        {
            await session.AbortTransactionAsync(cancellationToken);
            throw;
        }
    }
}
