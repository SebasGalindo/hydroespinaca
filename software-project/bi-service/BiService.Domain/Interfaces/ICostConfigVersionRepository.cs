using BiService.Domain.Entities;

namespace BiService.Domain.Interfaces;

/// <summary>
/// Contrato de persistencia para las versiones de configuración de costos.
/// </summary>
public interface ICostConfigVersionRepository
{
    /// <summary>Obtiene la versión de costos activa más reciente.</summary>
    /// <returns>La versión activa o <c>null</c> si no existe ninguna.</returns>
    Task<CostConfigVersion?> GetCurrentAsync(CancellationToken cancellationToken = default);

    /// <summary>Lista versiones de costos filtradas opcionalmente por rango de fecha de vigencia.</summary>
    /// <param name="from">Fecha mínima de <c>EffectiveFrom</c> (inclusive). Opcional.</param>
    /// <param name="to">Fecha máxima de <c>EffectiveFrom</c> (inclusive). Opcional.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task<IReadOnlyList<CostConfigVersion>> GetVersionsAsync(DateTime? from, DateTime? to, CancellationToken cancellationToken = default);

    /// <summary>Obtiene todas las versiones cuyo periodo de vigencia se solapa con el rango dado.</summary>
    /// <param name="from">Inicio del rango de consulta.</param>
    /// <param name="to">Fin del rango de consulta.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task<IReadOnlyList<CostConfigVersion>> GetVersionsForRangeAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default);

    /// <summary>Crea una nueva versión de costos, desactivando la anterior en una transacción.</summary>
    /// <param name="version">Entidad de la nueva versión a persistir.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>La versión creada con su ID asignado.</returns>
    Task<CostConfigVersion> CreateVersionAsync(CostConfigVersion version, CancellationToken cancellationToken = default);
}
