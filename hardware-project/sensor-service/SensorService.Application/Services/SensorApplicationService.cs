using FluentValidation;
using HydroEspinaca.Shared.DTOs.Sensors;
using HydroEspinaca.Shared.Errors;
using MongoDB.Bson;
using SensorService.Application.Interfaces;
using SensorService.Application.Mappers;
using SensorService.Domain.Interfaces;
using SensorService.Domain.Exceptions;

namespace SensorService.Application.Services;

public class SensorApplicationService : ISensorService
{
    private readonly ISensorRepository _repo;
    private readonly IEsp32NodeRepository _esp32NodeRepository;
    private readonly IVariableRepository _variableRepository;
    private readonly IValidator<SensorCreateDto> _createValidator;
    private readonly IValidator<SensorUpdateDto> _updateValidator;

    public SensorApplicationService(
        ISensorRepository repo,
        IEsp32NodeRepository esp32NodeRepository,
        IVariableRepository variableRepository,
        IValidator<SensorCreateDto> createValidator,
        IValidator<SensorUpdateDto> updateValidator
        )
    {
        _repo = repo;
        _esp32NodeRepository = esp32NodeRepository;
        _variableRepository = variableRepository;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<List<SensorDto>> GetAllAsync()
    {
        var sensors = await _repo.GetAllAsync();
        return sensors.Select(SensorMapper.ToDto).ToList();
    }

    public async Task<SensorDto?> GetByIdAsync(string id)
    {
        if (!ObjectId.TryParse(id, out _))
            throw new ValidationException("Formato de ID no válido. Se esperaba una cadena hexadecimal de 24 caracteres.");

        var sensor = await _repo.GetByIdAsync(id);
        if (sensor is null)
            throw new SensorNotFoundException(id);

        return SensorMapper.ToDto(sensor);
    }

    public async Task<SensorDto> CreateAsync(SensorCreateDto dto)
    {
        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var esp32Node = await _esp32NodeRepository.GetByIdAsync(dto.Esp32Id);
        if (esp32Node == null)
            throw new Esp32NotFoundException(dto.Esp32Id);

        var noExistingVariable = await _variableRepository.GetNonExistingCodesAsync(dto.Variables);
        if (noExistingVariable.Any())
            throw new InvalidOperationException($"Los siguientes códigos de variables no existen: {string.Join(", ", noExistingVariable)}");

        var sensorEntity = SensorMapper.ToEntity(dto);
        await _repo.CreateAsync(sensorEntity);
        return SensorMapper.ToDto(sensorEntity);
    }


    public async Task UpdateAsync(string id, SensorUpdateDto dto)
    {
        if (!ObjectId.TryParse(id, out _))
            throw new ValidationException("Formato de ID no válido. Se esperaba una cadena hexadecimal de 24 caracteres.");

        var validation = await _updateValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var existing = await _repo.GetByIdAsync(id);
        if (existing == null)
            throw new SensorNotFoundException(id);

        var esp32Node = await _esp32NodeRepository.GetByIdAsync(dto.Esp32Id);
        if (esp32Node == null)
            throw new Esp32NotFoundException(dto.Esp32Id);

        var noExistingVariable = await _variableRepository.GetNonExistingCodesAsync(dto.Variables);
        if (noExistingVariable.Any())
            throw new SensorDataNotFoundException($"Los siguientes códigos de variables no existen: {string.Join(", ", noExistingVariable)}");


        SensorMapper.MapUpdate(dto, existing);
        await _repo.UpdateAsync(existing);
    }

    public async Task DeleteAsync(string id)
    {
        if (!ObjectId.TryParse(id, out _))
            throw new ValidationException("Formato de ID no válido. Se esperaba una cadena hexadecimal de 24 caracteres.");

        var existing = await _repo.GetByIdAsync(id);
        if (existing == null)
            throw new SensorNotFoundException(id);

        await _repo.DeleteAsync(id);
    }
}
