using FluentValidation;
using HydroEspinaca.Shared.Errors;
using MongoDB.Bson;
using SensorService.Application.DTOs.Aggregate;
using SensorService.Application.Interfaces;
using SensorService.Application.Mappers;
using SensorService.Domain.Interfaces;

namespace SensorService.Application.Services;

public class AggregateService : IAggregateService
{
    private readonly IAggregateRepository _repo;
    private readonly ISensorRepository _sensorRepo;
    private readonly IVariableRepository _variableRepo;

    public AggregateService(
        IAggregateRepository repo,
        ISensorRepository sensorRepo,
        IVariableRepository variableRepo
        )
    {
        _repo = repo;
        _sensorRepo = sensorRepo;
        _variableRepo = variableRepo;
    }

    public async Task<List<AggregateDto>> GetBySensorAndVariableAsync(string sensorId, string variableId, DateTime from, DateTime to)
    {
        if (!ObjectId.TryParse(sensorId, out _))
            throw new ValidationException("Formato de ID no válido. Se esperaba una cadena hexadecimal de 24 caracteres.");

        if (from > to)
            throw new ValidationException("La fecha inicial no puede ser mayor a la final.");

        var sensor = await _sensorRepo.GetByIdAsync(sensorId);
        if (sensor is null)
            throw new NotFoundException($"Sensor con ID '{sensorId}' no encontrado.");

        var variable = await _variableRepo.GetByIdAsync(variableId);
        if (variable is null)
            throw new NotFoundException($"Variable con ID '{variableId}' no encontrada.");

        var results = await _repo.GetBySensorAndVariableAsync(sensorId, variableId, from, to);
        return results.Select(AggregateMapper.ToDto).ToList();
    }

}
