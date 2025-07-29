using HydroEspinaca.Shared.DTOs.Alerts;
using HydroEspinaca.Shared.Errors;
using MongoDB.Bson;
using SensorService.Application.DTOs.Alert;
using SensorService.Application.Interfaces;
using SensorService.Application.Mappers;
using SensorService.Domain.Interfaces;

namespace SensorService.Application.Services;

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

    public async Task<List<Esp32AlertDto>> GetByEsp32IdAsync(string esp32Id)
    {
        if (!ObjectId.TryParse(esp32Id, out _))
            throw new ArgumentException("Formato de ID no válido. Se esperaba una cadena hexadecimal de 24 caracteres.");

        var esp32 = await _esp32Repo.GetByIdAsync(esp32Id);
        if (esp32 is null)
            throw new NotFoundException($"ESP32 con ID '{esp32Id}' no encontrado.");

        var alerts = await _repo.GetByEsp32IdAsync(esp32Id);
        return alerts.Select(Esp32AlertMapper.ToDto).ToList();
    }

    public async Task AcknowledgeAsync(string id, Esp32AlertUpdateDto dto)
    {
        if (!ObjectId.TryParse(id, out _))
            throw new ArgumentException("Formato de ID no válido. Se esperaba una cadena hexadecimal de 24 caracteres.");

        var alert = await _repo.GetByIdAsync(id);
        if (alert is null)
            throw new NotFoundException($"Esp32Alert '{id}' no encontrado.");

        alert.Acknowledged = dto.Acknowledged;

        await _repo.UpdateAsync(alert);
    }
}
