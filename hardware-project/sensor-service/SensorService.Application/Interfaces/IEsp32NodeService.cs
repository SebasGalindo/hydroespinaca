using HydroEspinaca.Shared.DTOs.Esp32;

namespace SensorService.Application.Interfaces;

/// <summary>
/// Contrato del servicio de gestión de nodos ESP32 del sistema hidropónico IoT.
/// Administra el registro, consulta y actualización de estado de los microcontroladores.
/// </summary>
public interface IEsp32NodeService
{
    /// <summary>
    /// Obtiene todos los nodos ESP32 registrados en el sistema.
    /// </summary>
    /// <returns>Lista de todos los nodos ESP32.</returns>
    Task<List<Esp32NodeDto>> GetAllAsync();

    /// <summary>
    /// Obtiene un nodo ESP32 por su identificador único.
    /// </summary>
    /// <param name="id">Identificador del nodo ESP32.</param>
    /// <returns>DTO del nodo encontrado, o null si no existe.</returns>
    Task<Esp32NodeDto?> GetByIdAsync(string id);

    /// <summary>
    /// Registra un nuevo nodo ESP32 en el sistema.
    /// </summary>
    /// <param name="dto">DTO con los datos para la creación del nodo.</param>
    /// <returns>DTO del nodo ESP32 creado.</returns>
    Task<Esp32NodeDto> CreateAsync(Esp32NodeCreateDto dto);

    /// <summary>
    /// Actualiza el estado de un nodo ESP32 (activo, inactivo, etc.).
    /// </summary>
    /// <param name="id">Identificador del nodo ESP32.</param>
    /// <param name="dto">DTO con el nuevo estado del nodo.</param>
    Task UpdateStatusAsync(string id, Esp32NodeUpdateStatusDto dto);

    /// <summary>
    /// Verifica si un nodo ESP32 existe en el sistema.
    /// </summary>
    /// <param name="id">Identificador del nodo ESP32.</param>
    /// <returns>True si el nodo existe, false en caso contrario.</returns>
    Task<bool> ExistsAsync(string id);
}