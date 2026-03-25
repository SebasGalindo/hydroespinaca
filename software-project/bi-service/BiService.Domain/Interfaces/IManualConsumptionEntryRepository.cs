using BiService.Domain.Entities;
using BiService.Domain.Enums;

namespace BiService.Domain.Interfaces;

/// <summary>
/// Contrato de persistencia para los registros de consumo manual de recursos.
/// </summary>
public interface IManualConsumptionEntryRepository
{
    /// <summary>Obtiene un registro de consumo por su identificador.</summary>
    /// <param name="id">ID del registro.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>La entrada encontrada o <c>null</c>.</returns>
    Task<ManualConsumptionEntry?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>Crea un nuevo registro de consumo.</summary>
    /// <param name="entry">Entidad a persistir.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>La entrada creada con su ID asignado.</returns>
    Task<ManualConsumptionEntry> CreateAsync(ManualConsumptionEntry entry, CancellationToken cancellationToken = default);

    /// <summary>Obtiene registros de consumo dentro de un rango de fechas, con filtro opcional por tipo.</summary>
    /// <param name="from">Fecha de inicio (inclusive).</param>
    /// <param name="to">Fecha de fin (inclusive).</param>
    /// <param name="type">Tipo de consumo a filtrar. <c>null</c> para todos los tipos.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task<IReadOnlyList<ManualConsumptionEntry>> GetByRangeAsync(
        DateTime from,
        DateTime to,
        ConsumptionType? type,
        CancellationToken cancellationToken = default);

    /// <summary>Elimina un registro de consumo por su identificador.</summary>
    /// <param name="id">ID del registro a eliminar.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns><c>true</c> si se eliminó correctamente.</returns>
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
}
