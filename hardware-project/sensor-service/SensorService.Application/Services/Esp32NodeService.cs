using FluentValidation;
using HydroEspinaca.Shared.DTOs.Esp32;
using HydroEspinaca.Shared.Enums;
using HydroEspinaca.Shared.Errors;
using MongoDB.Bson;
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
        if (!ObjectId.TryParse(id, out _))
            throw new ArgumentException("Formato de ID no válido. Se esperaba una cadena hexadecimal de 24 caracteres.");

        var node = await _repo.GetByIdAsync(id);
        if (node is null)
            throw new NotFoundException($"ESP32 '{id}' no encontrado.");

        return Esp32NodeMapper.ToDto(node);
    }

    public async Task<Esp32NodeDto> CreateAsync(Esp32NodeCreateDto dto)
    {
        var validationResult = await _createValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var entity = Esp32NodeMapper.ToEntity(dto);
        await _repo.CreateAsync(entity);
        return Esp32NodeMapper.ToDto(entity);
    }

    public async Task UpdateStatusAsync(string id, Esp32NodeUpdateStatusDto dto)
    {
        if (!ObjectId.TryParse(id, out _))
            throw new ValidationException("Formato de ID no válido. Se esperaba una cadena hexadecimal de 24 caracteres.");

        var validationResult = await _statusValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        if (!Enum.TryParse<Esp32Status>(dto.Status, true, out var parsedStatus))
            throw new ArgumentException($"Estado '{dto.Status}' no es válido. Valores permitidos: {string.Join(", ", Enum.GetNames(typeof(Esp32Status)))}");

        var node = await _repo.GetByIdAsync(id);
        if (node is null)
            throw new NotFoundException($"ESP32 '{id}' no encontrado.");

        node.Status = parsedStatus;
        await _repo.UpdateStatusAsync(id, parsedStatus);
    }

    public async Task<bool> ExistsAsync(string id)
    {
        if (!ObjectId.TryParse(id, out _))
            throw new ValidationException("Formato de ID no válido. Se esperaba una cadena hexadecimal de 24 caracteres.");

        return await _repo.ExistsAsync(id);
    }
}
