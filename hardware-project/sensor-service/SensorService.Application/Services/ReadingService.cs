using FluentValidation;
using HydroEspinaca.Shared.Errors;
using MongoDB.Bson;
using SensorService.Application.DTOs.Reading;
using SensorService.Application.Interfaces;
using SensorService.Application.Mappers;
using SensorService.Domain.Interfaces;
using SensorService.Domain.Exceptions;

namespace SensorService.Application.Services;

public class ReadingService : IReadingService
{
    private readonly IReadingRepository _repo;
    private readonly ISensorRepository _sensorRepo;
    private readonly IVariableRepository _variableRepo;

    public ReadingService(
        IReadingRepository repo,
        ISensorRepository sensorRepo,
        IVariableRepository variableRepo
        )
    {
        _repo = repo;
        _sensorRepo = sensorRepo;
        _variableRepo = variableRepo;
    }

    public async Task<List<ReadingDto>> GetBySensorAndVariableAsync(string sensorId, string variableId, DateTime from, DateTime to)
    {
        if (!ObjectId.TryParse(sensorId, out _))
            throw new ValidationException("Formato de ID no válido. Se esperaba una cadena hexadecimal de 24 caracteres.");

        if (from > to)
            throw new ValidationException("La fecha inicial no puede ser mayor a la final.");

        var sensor = await _sensorRepo.GetByIdAsync(sensorId);
        if (sensor is null)
            throw new SensorNotFoundException($"Sensor con ID '{sensorId}' no encontrado.");

        var variable = await _variableRepo.GetByIdAsync(variableId);
        if (variable is null)
            throw new SensorDataNotFoundException($"Variable con ID no encontrada");

        var list = await _repo.GetBySensorAndVariableAsync(sensorId, variableId, from, to);
        return list.Select(ReadingMapper.ToDto).ToList();
    }
}