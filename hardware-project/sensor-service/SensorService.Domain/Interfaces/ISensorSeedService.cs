namespace SensorService.Domain.Interfaces;

/// <summary>
/// Servicio de inicialización de datos para los sensores del sistema hidropónico.
/// </summary>
public interface ISensorSeedService
{
    /// <summary>
    /// Inserta los sensores predeterminados en la base de datos si no existen.
    /// </summary>
    /// <returns>Cantidad de sensores insertados.</returns>
    Task<int> SeedDefaultSensorsAsync();
}
