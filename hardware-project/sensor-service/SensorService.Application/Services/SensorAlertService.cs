using FluentValidation;
using SensorService.Application.DTOs.Alert;
using SensorService.Application.Interfaces;
using SensorService.Application.Mappers;
using SensorService.Domain.Interfaces;

namespace SensorService.Application.Services;

public class SensorAlertService : ISensorAlertService
{
    private readonly ISensorAlertRepository _repo;
    private readonly IValidator<SensorAlertUpdateDto> _alertValidator;

    public SensorAlertService(
        ISensorAlertRepository repo,
        IValidator<SensorAlertUpdateDto> alertValidator
        )
    {
        _repo = repo;
        _alertValidator = alertValidator;
    }

    public async Task<List<SensorAlertDto>> GetBySensorIdAsync(string sensorId)
    {
        var list = await _repo.GetBySensorIdAsync(sensorId);
        return list.Select(SensorAlertMapper.ToDto).ToList();
    }

    public async Task AcknowledgeAsync(SensorAlertUpdateDto dto)
    {
        await _alertValidator.ValidateAndThrowAsync(dto);
        await _repo.UpdateAcknowledgedAsync(dto.Id, dto.Acknowledged);
    }
}
