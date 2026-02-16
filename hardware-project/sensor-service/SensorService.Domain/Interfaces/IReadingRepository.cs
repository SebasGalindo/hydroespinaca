using SensorService.Domain.Entities;

namespace SensorService.Domain.Interfaces;

/// <summary>
/// Repositorio para la persistencia y consulta de lecturas individuales de sensores.
/// </summary>
public interface IReadingRepository
{
    /// <summary>
    /// Crea una nueva lectura de sensor en la base de datos.
    /// </summary>
    /// <param name="reading">Lectura a persistir.</param>
    Task CreateAsync(Reading reading);

    /// <summary>
    /// Obtiene las lecturas de un sensor y variable específicos dentro de un rango de fechas.
    /// </summary>
    /// <param name="sensorCode">Código del sensor.</param>
    /// <param name="variableCode">Código de la variable ambiental.</param>
    /// <param name="from">Fecha de inicio del rango (inclusiva).</param>
    /// <param name="to">Fecha de fin del rango (inclusiva).</param>
    /// <returns>Lista de lecturas que coinciden con los criterios de búsqueda.</returns>
    Task<List<Reading>> GetBySensorAndVariableAsync(string sensorCode, string variableCode, DateTime from, DateTime to);

    /// <summary>
    /// Elimina lecturas anteriores a la fecha de corte especificada para limpieza periódica.
    /// </summary>
    /// <param name="cutoff">Fecha límite; se eliminan lecturas anteriores a esta fecha.</param>
    /// <returns>Cantidad de lecturas eliminadas.</returns>
    Task<int> DeleteOlderThanAsync(DateTime cutoff);

    /// <summary>
    /// Obtiene la lectura más reciente de cualquiera de los sensores especificados.
    /// </summary>
    /// <param name="sensorCodes">Lista de códigos de sensores a consultar.</param>
    /// <returns>La lectura más reciente o <c>null</c> si no existen lecturas.</returns>
    Task<Reading?> GetLatestBySensorCodesAsync(List<string> sensorCodes);

    /// <summary>
    /// Obtiene las últimas lecturas agrupadas por variable ambiental.
    /// </summary>
    /// <returns>Lista de las lecturas más recientes para cada variable.</returns>
    Task<List<Reading>> GetLatestReadingsByVariableAsync();
}
