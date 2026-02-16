namespace SensorService.Domain.Interfaces;

using SensorService.Domain.Entities;

/// <summary>
/// Repositorio para la persistencia y consulta de sensores físicos del sistema hidropónico.
/// </summary>
public interface ISensorRepository
{
    /// <summary>
    /// Obtiene un sensor por su identificador único.
    /// </summary>
    /// <param name="id">Identificador único del sensor.</param>
    /// <returns>El sensor encontrado o <c>null</c> si no existe.</returns>
    Task<Sensor?> GetByIdAsync(string id);

    /// <summary>
    /// Obtiene todos los sensores registrados en el sistema.
    /// </summary>
    /// <returns>Lista de todos los sensores.</returns>
    Task<List<Sensor>> GetAllAsync();

    /// <summary>
    /// Obtiene todos los sensores conectados a un nodo ESP32 específico.
    /// </summary>
    /// <param name="esp32Id">Identificador del nodo ESP32.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Lista de sensores del nodo ESP32.</returns>
    Task<List<Sensor>> GetSensorsByEsp32IdAsync(string esp32Id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Crea un nuevo sensor en la base de datos.
    /// </summary>
    /// <param name="sensor">Sensor a persistir.</param>
    Task CreateAsync(Sensor sensor);

    /// <summary>
    /// Actualiza los datos de un sensor existente.
    /// </summary>
    /// <param name="sensor">Sensor con los datos actualizados.</param>
    Task UpdateAsync(Sensor sensor);

    /// <summary>
    /// Elimina un sensor de la base de datos por su identificador.
    /// </summary>
    /// <param name="id">Identificador único del sensor a eliminar.</param>
    Task DeleteAsync(string id);
}
