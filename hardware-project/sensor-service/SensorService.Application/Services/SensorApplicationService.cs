using FluentValidation;
using HydroEspinaca.Shared.DTOs.Sensors;
using SensorService.Application.Interfaces;
using SensorService.Application.Mappers;
using HydroEspinaca.Shared.Enums;
using SensorService.Domain.Interfaces;

namespace SensorService.Application.Services;

public class SensorApplicationService : ISensorService
{
    private readonly ISensorRepository _repo;
    private readonly IValidator<SensorCreateDto> _createValidator;
    private readonly IValidator<SensorUpdateDto> _updateValidator;

    public SensorApplicationService(
        ISensorRepository repo,
        IValidator<SensorCreateDto> createValidator,
        IValidator<SensorUpdateDto> updateValidator
        )
    {
        _repo = repo;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<List<SensorDto>> GetAllAsync()
    {
        Console.WriteLine("Fetching all sensors from repository...");
        var sensors = await _repo.GetAllAsync();
        return sensors.Select(SensorMapper.ToDto).ToList();
    }

    public async Task<SensorDto?> GetByIdAsync(string id)
    {
        var sensor = await _repo.GetByIdAsync(id);
        return sensor is null ? null : SensorMapper.ToDto(sensor);
    }

    public async Task<string> CreateAsync(SensorCreateDto dto)
    {
        await _createValidator.ValidateAndThrowAsync(dto);
        var sensor = SensorMapper.ToEntity(dto);
        await _repo.CreateAsync(sensor);
        return sensor.Id!;
    }

    public async Task UpdateAsync(string id, SensorUpdateDto dto)
    {
        await _updateValidator.ValidateAndThrowAsync(dto);

        var existing = await _repo.GetByIdAsync(id);
        if (existing == null) throw new InvalidOperationException($"Sensor with id {id} not found");

        SensorMapper.MapUpdate(dto, existing);

        await _repo.UpdateAsync(existing);
    }

    public async Task DeleteAsync(string id)
    {
        await _repo.DeleteAsync(id);
    }
}
