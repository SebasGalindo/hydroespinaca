using BiService.Domain.Entities;

namespace BiService.Domain.Interfaces;

/// <summary>
/// Contrato de persistencia para los registros de producción de cultivos.
/// </summary>
public interface IProductionRecordRepository
{
    /// <summary>Obtiene un registro de producción por su identificador.</summary>
    /// <param name="id">ID del registro.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>El registro encontrado o <c>null</c>.</returns>
    Task<ProductionRecord?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>Obtiene todos los registros de producción ordenados por fecha de cosecha descendente.</summary>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task<IReadOnlyList<ProductionRecord>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Crea un nuevo registro de producción.</summary>
    /// <param name="record">Entidad a persistir.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>El registro creado con su ID asignado.</returns>
    Task<ProductionRecord> CreateAsync(ProductionRecord record, CancellationToken cancellationToken = default);

    /// <summary>Elimina un registro de producción por su identificador.</summary>
    /// <param name="id">ID del registro a eliminar.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns><c>true</c> si se eliminó correctamente.</returns>
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
}
