using SensorService.Application.Interfaces.UseCases.ProcessReadingBatch;
using SensorService.Domain.Interfaces;

namespace SensorService.Application.UseCases.ProcessReadingBatch;
public class ResolveOfflineAlertsUseCase : IResolveOfflineAlertsUseCase
{
    private readonly ISensorRepository _sensorRepository;
    private readonly ISensorAlertRepository _alertRepository;

    public ResolveOfflineAlertsUseCase(
        ISensorRepository sensorRepository,
        ISensorAlertRepository alertRepository)
    {
        _sensorRepository = sensorRepository;
        _alertRepository = alertRepository;
    }

    public async Task ExecuteAsync(string esp32Id)
    {
        var allSensors = await _sensorRepository.GetAllAsync();
        var anySensor = allSensors.FirstOrDefault(s => s.Esp32Id == esp32Id);

        if (anySensor == null) return;

        var offlineAlerts = (await _alertRepository.GetBySensorIdAsync(anySensor.Id!))
            .Where(a => a.Type == "Esp32Offline" && !a.Acknowledged)
            .ToList();

        foreach (var alert in offlineAlerts)
        {
            await _alertRepository.UpdateAcknowledgedAsync(alert.Id, true);
        }
    }
}