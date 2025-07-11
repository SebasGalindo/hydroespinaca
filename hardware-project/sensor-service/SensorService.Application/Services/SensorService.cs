using SensorService.Application.DTOs;
using SensorService.Application.Interfaces;
using SensorService.Domain.Entities;
using SensorService.Domain.Enums;
using SensorService.Domain.Interfaces;

namespace SensorService.Application.Services;

public class SensorService : ISensorService
{
    private readonly ISensorRepository _repo;

    public SensorService(ISensorRepository repo)
    {
        _repo = repo;
    }

    public async Task<List<SensorDto>> GetAllAsync()
    {
        var sensors = await _repo.GetAllAsync();
        return sensors.Select(MapToDto).ToList();
    }

    public async Task<SensorDto?> GetByIdAsync(string id)
    {
        var sensor = await _repo.GetByIdAsync(id);
        return sensor is null ? null : MapToDto(sensor);
    }

    public async Task<string> CreateAsync(SensorCreateDto dto)
    {
        var sensor = new Sensor
        {
            Code = dto.Code,
            PhysicalId = dto.PhysicalId,
            Location = dto.Location,
            Esp32Id = dto.Esp32Id,
            SamplingFrequency = dto.SamplingFrequency,
            Variables = dto.Variables,
            CreatedAt = DateTime.UtcNow,
            Status = SensorStatus.Active
        };

        await _repo.CreateAsync(sensor);
        return sensor.Id!;
    }

    public async Task UpdateAsync(string id, SensorUpdateDto dto)
    {
        var existing = await _repo.GetByIdAsync(id);
        if (existing == null)
            throw new KeyNotFoundException($"Sensor with id {id} not found");

        existing.PhysicalId = dto.PhysicalId;
        existing.Location = dto.Location;
        existing.Esp32Id = dto.Esp32Id;
        existing.SamplingFrequency = dto.SamplingFrequency;
        existing.Variables = dto.Variables;
        existing.Status = Enum.Parse<SensorStatus>(dto.Status, ignoreCase: true);

        await _repo.UpdateAsync(existing);
    }

    public async Task DeleteAsync(string id)
    {
        await _repo.DeleteAsync(id);
    }

    private static SensorDto MapToDto(Sensor sensor) => new()
    {
        Id = sensor.Id,
        Code = sensor.Code,
        PhysicalId = sensor.PhysicalId,
        Location = sensor.Location,
        Esp32Id = sensor.Esp32Id,
        SamplingFrequency = sensor.SamplingFrequency,
        Variables = sensor.Variables,
        Status = sensor.Status.ToString(),
        CreatedAt = sensor.CreatedAt
    };
}
