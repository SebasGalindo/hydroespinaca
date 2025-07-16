using SensorService.Application.DTOs.Alert;
using SensorService.Application.Interfaces;
using SensorService.Domain.Interfaces;

namespace SensorService.Application.Services;

public class SensorAlertService : ISensorAlertService
{
    private readonly ISensorAlertRepository _repo;

    public SensorAlertService(ISensorAlertRepository repo)
    {
        _repo = repo;
    }

    public async Task<List<SensorAlertDto>> GetBySensorIdAsync(string sensorId)
    {
        var list = await _repo.GetBySensorIdAsync(sensorId);
        return list.Select(x => new SensorAlertDto
        {
            Id = x.Id,
            SensorId = x.SensorId,
            Type = x.Type,
            Value = x.Value,
            Threshold = x.Threshold,
            Timestamp = x.Timestamp,
            Message = x.Message,
            Severity = x.Severity,
            Acknowledged = x.Acknowledged
        }).ToList();
    }

    public async Task AcknowledgeAsync(string alertId, bool acknowledged)
    {
        await _repo.UpdateAcknowledgedAsync(alertId, acknowledged);
    }
}
