using FluentValidation;
using HydroEspinaca.Shared.DTOs.Esp32;
using HydroEspinaca.Shared.Enums;
using HydroEspinaca.Shared.Errors;
using MongoDB.Bson;
using SensorService.Application.Interfaces;
using SensorService.Application.Mappers;
using SensorService.Domain.Interfaces;
using SensorService.Domain.Exceptions;

namespace SensorService.Application.Services;

/// <summary>
/// Servicio de aplicación para la gestión de nodos ESP32 del sistema hidropónico IoT.
/// Administra el ciclo de vida de los microcontroladores incluyendo registro, consulta,
/// actualización de estado y verificación de existencia. Utiliza validación con FluentValidation.
/// </summary>
public class Esp32NodeService : IEsp32NodeService
{
    private readonly IEsp32NodeRepository _repo;
    private readonly IValidator<Esp32NodeCreateDto> _createValidator;
    private readonly IValidator<Esp32NodeUpdateStatusDto> _statusValidator;

    public Esp32NodeService(
        IEsp32NodeRepository repo,
        IValidator<Esp32NodeCreateDto> createValidator,
        IValidator<Esp32NodeUpdateStatusDto> statusValidator
        )
    {
        _repo = repo;
        _createValidator = createValidator;
        _statusValidator = statusValidator;
    }

    /// <summary>
    /// Obtiene todos los nodos ESP32 registrados en el sistema.
    /// </summary>
    /// <returns>Lista de DTOs de todos los nodos ESP32.</returns>
    public async Task<List<Esp32NodeDto>> GetAllAsync()
    {
        var nodes = await _repo.GetAllAsync();
        return nodes.Select(Esp32NodeMapper.ToDto).ToList();
    }

    /// <summary>
    /// Obtiene un nodo ESP32 por su identificador único.
    /// </summary>
    /// <param name="id">Identificador del nodo (ObjectId de 24 caracteres).</param>
    /// <returns>DTO del nodo encontrado.</returns>
    public async Task<Esp32NodeDto?> GetByIdAsync(string id)
    {
        if (!ObjectId.TryParse(id, out _))
            throw new ArgumentException("Formato de ID no válido. Se esperaba una cadena hexadecimal de 24 caracteres.");

        var node = await _repo.GetByIdAsync(id);
        if (node is null)
            throw new Esp32NotFoundException(id);

        return Esp32NodeMapper.ToDto(node);
    }

    /// <summary>
    /// Registra un nuevo nodo ESP32 en el sistema tras validar los datos de entrada.
    /// </summary>
    /// <param name="dto">DTO con los datos de creación del nodo.</param>
    /// <returns>DTO del nodo ESP32 creado.</returns>
    public async Task<Esp32NodeDto> CreateAsync(Esp32NodeCreateDto dto)
    {
        var validationResult = await _createValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var entity = Esp32NodeMapper.ToEntity(dto);
        await _repo.CreateAsync(entity);
        return Esp32NodeMapper.ToDto(entity);
    }

    /// <summary>
    /// Actualiza el estado de un nodo ESP32 (activo, inactivo, offline, etc.).
    /// Valida el formato del ID, los datos de entrada y que el estado sea válido.
    /// </summary>
    /// <param name="id">Identificador del nodo (ObjectId de 24 caracteres).</param>
    /// <param name="dto">DTO con el nuevo estado.</param>
    public async Task UpdateStatusAsync(string id, Esp32NodeUpdateStatusDto dto)
    {
        if (!ObjectId.TryParse(id, out _))
            throw new ValidationException("Formato de ID no válido. Se esperaba una cadena hexadecimal de 24 caracteres.");

        var validationResult = await _statusValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        if (!Enum.TryParse<Esp32Status>(dto.Status, true, out var parsedStatus))
            throw new ValidationException($"Estado '{dto.Status}' no es válido. Valores permitidos: {string.Join(", ", Enum.GetNames(typeof(Esp32Status)))}");

        var updated = await _repo.UpdateStatusAsync(id, parsedStatus);
        if (!updated)
            throw new SensorDataNotFoundException("No se pudo actualizar el estado");
    }


    /// <summary>
    /// Verifica si un nodo ESP32 existe en el sistema.
    /// </summary>
    /// <param name="id">Identificador del nodo (ObjectId de 24 caracteres).</param>
    /// <returns>True si el nodo existe, false en caso contrario.</returns>
    public async Task<bool> ExistsAsync(string id)
    {
        if (!ObjectId.TryParse(id, out _))
            throw new ValidationException("Formato de ID no válido. Se esperaba una cadena hexadecimal de 24 caracteres.");

        return await _repo.ExistsAsync(id);
    }
}
