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
    /// Obtiene la versión de configuración de costos vigente en la fecha actual,
    /// determinada por EffectiveFrom &lt;= ahora y (EffectiveTo &gt;= ahora o EffectiveTo nulo).
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>La versión vigente actual, o <c>null</c> si no existe ninguna.</returns>
    public async Task<CostConfigVersion?> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var filter = Builders<CostConfigVersionDocument>.Filter.And(
            Builders<CostConfigVersionDocument>.Filter.Lte(x => x.EffectiveFrom, now),
            Builders<CostConfigVersionDocument>.Filter.Or(
                Builders<CostConfigVersionDocument>.Filter.Gte(x => x.EffectiveTo, now),
                Builders<CostConfigVersionDocument>.Filter.Eq(x => x.EffectiveTo, null)
            )
        );
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
    /// Crea una nueva versión de configuración de costos y recalcula EffectiveTo e IsActive
    /// de todas las versiones dentro de una transacción.
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
            // Insert the new version
            var newDoc = _mapper.ToDocument(version);
            await _collection.InsertOneAsync(session, newDoc, cancellationToken: cancellationToken);
            version.SetId(newDoc.Id);

            // Recalculate all versions: get all sorted by EffectiveFrom ascending
            var allDocs = await _collection
                .Find(session, Builders<CostConfigVersionDocument>.Filter.Empty)
                .Sort(Builders<CostConfigVersionDocument>.Sort.Ascending(x => x.EffectiveFrom))
                .ToListAsync(cancellationToken);

            var now = DateTime.UtcNow;

            for (var i = 0; i < allDocs.Count; i++)
            {
                var doc = allDocs[i];

                // Auto-calculate EffectiveTo: one day before the next version's EffectiveFrom,
                // unless the user manually set EffectiveTo and it's earlier.
                DateTime? autoEffectiveTo = null;
                if (i < allDocs.Count - 1)
                {
                    autoEffectiveTo = allDocs[i + 1].EffectiveFrom.AddDays(-1);
                }

                // If EffectiveTo was manually set by user (already in DB for existing records)
                // and it's earlier than the auto-calculated value, keep the manual value.
                // For the newly inserted doc, respect EffectiveTo if it was provided.
                DateTime? finalEffectiveTo;
                if (i < allDocs.Count - 1)
                {
                    // Not the last version: must have an end date
                    if (doc.EffectiveTo.HasValue && doc.EffectiveTo.Value < autoEffectiveTo)
                    {
                        // User set a tighter end date, keep it
                        finalEffectiveTo = doc.EffectiveTo;
                    }
                    else
                    {
                        finalEffectiveTo = autoEffectiveTo;
                    }
                }
                else
                {
                    // Last (most recent) version: keep EffectiveTo if user set it, otherwise null
                    finalEffectiveTo = doc.EffectiveTo;
                }

                // Determine IsActive based on date coverage of "now"
                var isActive = doc.EffectiveFrom <= now
                    && (!finalEffectiveTo.HasValue || finalEffectiveTo.Value >= now);

                // Update if any field changed
                if (doc.EffectiveTo != finalEffectiveTo || doc.IsActive != isActive)
                {
                    var update = Builders<CostConfigVersionDocument>.Update
                        .Set(x => x.EffectiveTo, finalEffectiveTo)
                        .Set(x => x.IsActive, isActive);

                    await _collection.UpdateOneAsync(
                        session,
                        Builders<CostConfigVersionDocument>.Filter.Eq(x => x.Id, doc.Id),
                        update,
                        cancellationToken: cancellationToken);
                }

                // Update local reference for the newly created version
                if (doc.Id == newDoc.Id)
                {
                    version.EffectiveTo = finalEffectiveTo;
                    version.IsActive = isActive;
                }
            }

            await session.CommitTransactionAsync(cancellationToken);
            return version;
        }
        catch
        {
            await session.AbortTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task<CostConfigVersion?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var filter = Builders<CostConfigVersionDocument>.Filter.Eq(x => x.Id, id);
        var doc = await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
        return doc is null ? null : _mapper.ToEntity(doc);
    }

    public async Task<CostConfigVersion> UpdateVersionAsync(
        CostConfigVersion version, CancellationToken cancellationToken = default)
    {
        using var session = await _collection.Database.Client.StartSessionAsync(cancellationToken: cancellationToken);
        session.StartTransaction();

        try
        {
            var doc = _mapper.ToDocument(version);
            var filter = Builders<CostConfigVersionDocument>.Filter.Eq(x => x.Id, doc.Id);
            var update = Builders<CostConfigVersionDocument>.Update
                .Set(x => x.Currency, doc.Currency)
                .Set(x => x.ElectricityCostPerKwh, doc.ElectricityCostPerKwh)
                .Set(x => x.WaterCostPerLiter, doc.WaterCostPerLiter)
                .Set(x => x.NutrientCostPerLiter, doc.NutrientCostPerLiter)
                .Set(x => x.EffectiveFrom, doc.EffectiveFrom)
                .Set(x => x.EffectiveTo, doc.EffectiveTo);

            await _collection.UpdateOneAsync(session, filter, update, cancellationToken: cancellationToken);

            await RecalculateAllVersionsAsync(session, cancellationToken);

            // Re-read the updated version to get the recalculated values
            var updated = await _collection.Find(session, filter).FirstOrDefaultAsync(cancellationToken)
                ?? throw new InvalidOperationException("Version not found after update");
            var entity = _mapper.ToEntity(updated);

            await session.CommitTransactionAsync(cancellationToken);
            return entity;
        }
        catch
        {
            await session.AbortTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task<bool> DeleteVersionAsync(string id, CancellationToken cancellationToken = default)
    {
        using var session = await _collection.Database.Client.StartSessionAsync(cancellationToken: cancellationToken);
        session.StartTransaction();

        try
        {
            var filter = Builders<CostConfigVersionDocument>.Filter.Eq(x => x.Id, id);
            var result = await _collection.DeleteOneAsync(session, filter, cancellationToken: cancellationToken);

            if (result.DeletedCount == 0)
            {
                await session.AbortTransactionAsync(cancellationToken);
                return false;
            }

            await RecalculateAllVersionsAsync(session, cancellationToken);

            await session.CommitTransactionAsync(cancellationToken);
            return true;
        }
        catch
        {
            await session.AbortTransactionAsync(cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// Recalcula EffectiveTo e IsActive de todas las versiones dentro de una transacción.
    /// </summary>
    private async Task RecalculateAllVersionsAsync(
        IClientSessionHandle session, CancellationToken cancellationToken)
    {
        var allDocs = await _collection
            .Find(session, Builders<CostConfigVersionDocument>.Filter.Empty)
            .Sort(Builders<CostConfigVersionDocument>.Sort.Ascending(x => x.EffectiveFrom))
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;

        for (var i = 0; i < allDocs.Count; i++)
        {
            var doc = allDocs[i];

            DateTime? autoEffectiveTo = null;
            if (i < allDocs.Count - 1)
                autoEffectiveTo = allDocs[i + 1].EffectiveFrom.AddDays(-1);

            DateTime? finalEffectiveTo;
            if (i < allDocs.Count - 1)
            {
                if (doc.EffectiveTo.HasValue && doc.EffectiveTo.Value < autoEffectiveTo)
                    finalEffectiveTo = doc.EffectiveTo;
                else
                    finalEffectiveTo = autoEffectiveTo;
            }
            else
            {
                finalEffectiveTo = doc.EffectiveTo;
            }

            var isActive = doc.EffectiveFrom <= now
                && (!finalEffectiveTo.HasValue || finalEffectiveTo.Value >= now);

            if (doc.EffectiveTo != finalEffectiveTo || doc.IsActive != isActive)
            {
                var upd = Builders<CostConfigVersionDocument>.Update
                    .Set(x => x.EffectiveTo, finalEffectiveTo)
                    .Set(x => x.IsActive, isActive);

                await _collection.UpdateOneAsync(
                    session,
                    Builders<CostConfigVersionDocument>.Filter.Eq(x => x.Id, doc.Id),
                    upd,
                    cancellationToken: cancellationToken);
            }
        }
    }
}
