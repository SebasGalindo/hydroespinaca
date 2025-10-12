using FluentValidation;
using HydroEspinaca.Shared.DTOs.Alerts;
using HydroEspinaca.Shared.Errors;
using MongoDB.Bson;
using SensorService.Application.DTOs.Alert;
using SensorService.Application.Interfaces;
using SensorService.Application.Mappers;
using SensorService.Domain.Entities;
using SensorService.Domain.Interfaces;
using SensorService.Domain.Exceptions;

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
            throw new SensorNotFoundException($"Sensor con ID '{sensorId}' no encontrado.");

        // Use sensor Code for querying alerts
        var list = await _repo.GetBySensorCodeAsync(sensor.Code);
        return list.Select(SensorAlertMapper.ToDto).ToList();
    }

    public async Task AcknowledgeAsync(string id, SensorAlertUpdateDto dto)
    {
        if (!ObjectId.TryParse(id, out _))
            throw new ValidationException("Formato de ID no válido. Se esperaba una cadena hexadecimal de 24 caracteres.");

        var sensorAlert = await _repo.GetByIdAsync(id);
        if (sensorAlert is null)
            throw new SensorDataNotFoundException("Alert no encontrado");

        sensorAlert.Acknowledged = dto.Acknowledged;

        await _repo.UpdateAsync(sensorAlert);
    }
}
