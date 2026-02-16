namespace SensorService.Domain.Interfaces;

/// <summary>
/// Servicio de inicialización de datos para las variables ambientales del sistema hidropónico.
/// </summary>
public interface IVariableSeedService
{
    /// <summary>
    /// Inserta las variables ambientales predeterminadas en la base de datos si no existen.
    /// </summary>
    /// <returns>Cantidad de variables insertadas.</returns>
    Task<int> SeedDefaultVariablesAsync();
}
