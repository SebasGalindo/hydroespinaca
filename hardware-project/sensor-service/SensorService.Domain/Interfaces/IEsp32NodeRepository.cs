using HydroEspinaca.Shared.Enums;
using SensorService.Domain.Entities;

namespace SensorService.Domain.Interfaces;

/// <summary>
/// Repositorio para la persistencia y consulta de nodos microcontroladores ESP32.
/// </summary>
public interface IEsp32NodeRepository
{
    /// <summary>
    /// Obtiene todos los nodos ESP32 registrados en el sistema.
    /// </summary>
    /// <returns>Lista de todos los nodos ESP32.</returns>
    Task<List<Esp32Node>> GetAllAsync();

    /// <summary>
    /// Obtiene un nodo ESP32 por su identificador único.
    /// </summary>
    /// <param name="id">Identificador único del nodo.</param>
    /// <returns>El nodo encontrado o <c>null</c> si no existe.</returns>
    Task<Esp32Node?> GetByIdAsync(string id);

    /// <summary>
    /// Obtiene un nodo ESP32 por su identificador lógico (nombre o código).
    /// </summary>
    /// <param name="identifier">Identificador lógico del nodo.</param>
    /// <returns>El nodo encontrado o <c>null</c> si no existe.</returns>
    Task<Esp32Node?> GetByIdentifierAsync(string identifier);

    /// <summary>
    /// Crea un nuevo nodo ESP32 en la base de datos.
    /// </summary>
    /// <param name="node">Nodo ESP32 a persistir.</param>
    Task CreateAsync(Esp32Node node);

    /// <summary>
    /// Actualiza los datos de un nodo ESP32 existente.
    /// </summary>
    /// <param name="node">Nodo ESP32 con los datos actualizados.</param>
    Task UpdateAsync(Esp32Node node);

    /// <summary>
    /// Actualiza únicamente el estado de un nodo ESP32.
    /// </summary>
    /// <param name="id">Identificador único del nodo.</param>
    /// <param name="status">Nuevo estado del nodo.</param>
    /// <returns><c>true</c> si la actualización fue exitosa; de lo contrario, <c>false</c>.</returns>
    Task<bool> UpdateStatusAsync(string id, Esp32Status status);

    /// <summary>
    /// Verifica si existe un nodo ESP32 con el identificador especificado.
    /// </summary>
    /// <param name="id">Identificador único del nodo.</param>
    /// <returns><c>true</c> si el nodo existe; de lo contrario, <c>false</c>.</returns>
    Task<bool> ExistsAsync(string id);

    /// <summary>
    /// Actualiza la fecha y hora de última actividad de un nodo ESP32.
    /// </summary>
    /// <param name="id">Identificador único del nodo.</param>
    /// <param name="lastSeen">Nueva fecha y hora de última actividad.</param>
    /// <returns><c>true</c> si la actualización fue exitosa; de lo contrario, <c>false</c>.</returns>
    Task<bool> UpdateLastSeenAsync(string id, DateTime lastSeen);
}