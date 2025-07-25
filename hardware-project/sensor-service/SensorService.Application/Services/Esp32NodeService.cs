using FluentValidation;
using HydroEspinaca.Shared.DTOs.Esp32;
using SensorService.Application.Interfaces;
using SensorService.Application.Mappers;
using SensorService.Domain.Interfaces;

namespace SensorService.Application.Services;

public class Esp32NodeService : IEsp32NodeService
{
    private readonly IEsp32NodeRepository _repo;
    private readonly IValidator<Esp32NodeCreateDto> _createValidator;
    private readonly IValidator<Esp32NodeUpdateStatusDto> _statusValidator;

    public Esp32NodeService(
        IEsp32NodeRepository repo,
        IValidator<Esp32NodeCreateDto> createValidator,
        IValidator<Esp32NodeUpdateStatusDto> statusValidator
        )
    {
        _repo = repo;
        _createValidator = createValidator;
        _statusValidator = statusValidator;
    }

    public async Task<List<Esp32NodeDto>> GetAllAsync()
    {
        var nodes = await _repo.GetAllAsync();
        return nodes.Select(Esp32NodeMapper.ToDto).ToList();
    }

    public async Task<Esp32NodeDto?> GetByIdAsync(string id)
    {
        var node = await _repo.GetByIdAsync(id);
        return node is null ? null : Esp32NodeMapper.ToDto(node);
    }

    public async Task CreateAsync(Esp32NodeCreateDto dto)
    {
        await _createValidator.ValidateAndThrowAsync(dto);

        var exists = await _repo.GetByIdAsync(dto.Id);
        if (exists != null)
            throw new InvalidOperationException($"El ESP32 con ID '{dto.Id}' ya existe.");

        var entity = Esp32NodeMapper.ToEntity(dto);

        await _repo.CreateAsync(entity);
    }

    public async Task UpdateStatusAsync(string id, Esp32NodeUpdateStatusDto dto)
    {
        await _statusValidator.ValidateAndThrowAsync(dto);

        var node = await _repo.GetByIdAsync(id);
        if (node is null)
            throw new InvalidOperationException($"ESP32 '{id}' no encontrado.");

        node.Status = dto.Status;
        await _repo.UpdateStatusAsync(id, dto.Status);
    }

}
