using Microsoft.Extensions.Logging;
using SensorService.Application.Interfaces.UseCases.Esp32;
using SensorService.Domain.Interfaces;

namespace SensorService.Application.UseCases.Esp32;

/// <summary>
/// Caso de uso para actualizar la marca de tiempo de última actividad de un nodo ESP32.
/// Se ejecuta cada vez que se recibe un lote de lecturas válido, manteniendo
/// actualizado el registro de conectividad del dispositivo.
/// </summary>
public class UpdateEsp32LastSeenUseCase : IUpdateEsp32LastSeenUseCase
{
    private readonly IEsp32NodeRepository _esp32NodeRepository;
    private readonly ILogger<UpdateEsp32LastSeenUseCase> _logger;

    public UpdateEsp32LastSeenUseCase(
        IEsp32NodeRepository esp32NodeRepository,
        ILogger<UpdateEsp32LastSeenUseCase> logger)
    {
        _esp32NodeRepository = esp32NodeRepository;
        _logger = logger;
    }

    /// <summary>
    /// Actualiza la marca de tiempo de última actividad del ESP32 en el repositorio.
    /// Si el nodo no existe o la marca no cambió, registra una advertencia.
    /// </summary>
    /// <param name="esp32Id">Identificador del ESP32.</param>
    /// <param name="timestamp">Nueva marca de tiempo de actividad.</param>
    public async Task ExecuteAsync(string esp32Id, DateTime timestamp)
    {
        var updated = await _esp32NodeRepository.UpdateLastSeenAsync(esp32Id, timestamp);
        if (!updated)
            _logger.LogWarning("ESP32 node with ID '{Esp32Id}' not found or lastSeen unchanged.", esp32Id);
    }
}