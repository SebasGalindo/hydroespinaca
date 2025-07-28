using FluentValidation;
using HydroEspinaca.Shared.DTOs.Alerts;
using HydroEspinaca.Shared.Errors;
using MongoDB.Bson;
using SensorService.Application.DTOs.Alert;
using SensorService.Application.Interfaces;
using SensorService.Application.Mappers;
using SensorService.Domain.Entities;
using SensorService.Domain.Interfaces;

namespace SensorService.Application.Services;

public class SensorAlertService : ISensorAlertService
{
    private readonly ISensorAlertRepository _repo;
    private readonly ISensorRepository _sensorRepo;

    public SensorAlertService(
        ISensorAlertRepository repo,
        ISensorRepository sensorRepo
        )
    {
        _repo = repo;
        _sensorRepo = sensorRepo;
    }

    public async Task<List<SensorAlertDto>> GetBySensorIdAsync(string sensorId)
    {

        if (!ObjectId.TryParse(sensorId, out _))
            throw new ValidationException("Formato de ID no válido. Se esperaba una cadena hexadecimal de 24 caracteres.");

        var sensor = await _sensorRepo.GetByIdAsync(sensorId);
        if (sensor is null)
            throw new NotFoundException($"Sensor con ID '{sensorId}' no encontrado.");

        var list = await _repo.GetBySensorIdAsync(sensorId);
        return list.Select(SensorAlertMapper.ToDto).ToList();
    }

    public async Task AcknowledgeAsync(string id, SensorAlertUpdateDto dto)
    {
        if (!ObjectId.TryParse(id, out _))
            throw new ValidationException("Formato de ID no válido. Se esperaba una cadena hexadecimal de 24 caracteres.");

        var sensorAlert = await _repo.GetByIdAsync(id);
        if (sensorAlert is null)
            throw new NotFoundException($"SensorAlert '{sensorAlert}' no encontrado.");

        await _repo.UpdateAcknowledgedAsync(id, dto.Acknowledged);
    }
}
