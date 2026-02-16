using HydroEspinaca.Shared.DTOs.Alerts;
using HydroEspinaca.Shared.Errors;
using MongoDB.Bson;
using SensorService.Application.DTOs.Alert;
using SensorService.Application.Interfaces;
using SensorService.Application.Mappers;
using SensorService.Domain.Interfaces;
using SensorService.Domain.Exceptions;

namespace SensorService.Application.Services;

/// <summary>
/// Servicio de aplicación para la gestión de alertas de dispositivos ESP32.
/// Permite consultar alertas por ESP32 y actualizar su estado de reconocimiento.
/// </summary>
public class Esp32AlertService : IEsp32AlertService
{
    private readonly IEsp32AlertRepository _repo;
    private readonly IEsp32NodeRepository _esp32Repo;

    public Esp32AlertService(
        IEsp32AlertRepository repo,
        IEsp32NodeRepository esp32Repo
    )
    {
        _repo = repo;
        _esp32Repo = esp32Repo;
    }

    /// <summary>
    /// Obtiene todas las alertas asociadas a un dispositivo ESP32.
    /// Valida el formato del ID y la existencia del dispositivo.
    /// </summary>
    /// <param name="esp32Id">Identificador del ESP32 (ObjectId de 24 caracteres).</param>
    /// <returns>Lista de alertas del ESP32.</returns>
    public async Task<List<Esp32AlertDto>> GetByEsp32IdAsync(string esp32Id)
    {
        if (!ObjectId.TryParse(esp32Id, out _))
            throw new ArgumentException("Formato de ID no válido. Se esperaba una cadena hexadecimal de 24 caracteres.");

        var esp32 = await _esp32Repo.GetByIdAsync(esp32Id);
        if (esp32 is null)
            throw new Esp32NotFoundException(esp32Id);

        var alerts = await _repo.GetByEsp32IdAsync(esp32Id);
        return alerts.Select(Esp32AlertMapper.ToDto).ToList();
    }

    /// <summary>
    /// Actualiza el estado de reconocimiento de una alerta de ESP32.
    /// Valida el formato del ID y la existencia de la alerta.
    /// </summary>
    /// <param name="id">Identificador de la alerta (ObjectId de 24 caracteres).</param>
    /// <param name="dto">DTO con el nuevo estado de reconocimiento.</param>
    public async Task AcknowledgeAsync(string id, Esp32AlertUpdateDto dto)
    {
        if (!ObjectId.TryParse(id, out _))
            throw new ArgumentException("Formato de ID no válido. Se esperaba una cadena hexadecimal de 24 caracteres.");

        var alert = await _repo.GetByIdAsync(id);
        if (alert is null)
            throw new SensorDataNotFoundException("Alert no encontrado");

        alert.Acknowledged = dto.Acknowledged;

        await _repo.UpdateAsync(alert);
    }
}
