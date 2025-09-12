using SensorService.Domain.Entities;
using HydroEspinaca.Shared.Enums;

namespace SensorService.Domain.Interfaces;

public interface IEsp32AlertRepository
{
    Task<Esp32Alert?> GetUnacknowledgedByEsp32AndTypeAsync(string esp32Id, AlertType type);
    Task CreateAsync(Esp32Alert alert);
    Task UpdateAsync(Esp32Alert alert);
    Task<Esp32Alert?> GetByIdAsync(string id);
    Task<List<Esp32Alert>> GetByEsp32IdAsync(string esp32Id);
}
