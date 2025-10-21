using ActuatorService.Application.DTOs;
using ActuatorService.Application.Services;
using ActuatorService.Domain.Entities;
using ActuatorService.Domain.Interfaces;
using HydroEspinaca.Shared.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ActuatorService.Api.Controllers;

[ApiController]
[Route("api/internal-routines")]
public class InternalRoutinesController : ControllerBase
{
    private readonly IInternalRoutineRepository _repository;
    private readonly IActuatorRepository _actuatorRepository;
    private readonly ILogger<InternalRoutinesController> _logger;

    public InternalRoutinesController(
        IInternalRoutineRepository repository,
        IActuatorRepository actuatorRepository,
        ILogger<InternalRoutinesController> logger)
    {
        _repository = repository;
        _actuatorRepository = actuatorRepository;
        _logger = logger;
    }

    /// <summary>
    /// Get all internal routines
    /// </summary>
    [HttpGet]
    [Authorize(Policy = PolicyNames.CommandRead)]
    public async Task<IActionResult> GetAll([FromQuery] bool activeOnly = false)
    {
        var routines = activeOnly
            ? await _repository.GetActiveRoutinesAsync()
            : (await _repository.GetActiveRoutinesAsync())
                .Concat(await GetInactiveRoutinesAsync())
                .ToList();

        var dtos = routines.Select(MapToDto).ToList();
        return Ok(dtos);
    }

    /// <summary>
    /// Get internal routine by ID
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Policy = PolicyNames.CommandRead)]
    public async Task<IActionResult> GetById(string id)
    {
        var routine = await _repository.GetByIdAsync(id);

        if (routine == null)
            return NotFound(new { Message = $"Internal routine with ID {id} not found" });

        return Ok(MapToDto(routine));
    }

    /// <summary>
    /// Create a new internal routine
    /// </summary>
    [HttpPost]
    [Authorize(Policy = PolicyNames.CommandCreate)]
    public async Task<IActionResult> Create([FromBody] CreateInternalRoutineDto dto)
    {
        // Validate that all OutputVariables (ActuatorCodes) exist
        var allActuators = await _actuatorRepository.GetAllAsync();
        foreach (var step in dto.Steps)
        {
            var actuator = allActuators.FirstOrDefault(a => a.Code == step.OutputVariable);
            if (actuator == null)
            {
                return BadRequest(new
                {
                    Message = $"Actuator with code '{step.OutputVariable}' not found"
                });
            }
        }

        var routine = new InternalRoutine
        {
            Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
            Name = dto.Name,
            Description = dto.Description,
            Esp32Id = dto.Esp32Id,
            Interval = dto.Interval,
            IsActive = dto.IsActive,
            Steps = dto.Steps.Select(s => new InternalRoutineStep
            {
                OutputVariable = s.OutputVariable,
                Power = s.Power,
                Duration = s.Duration,
                DutyCycle = s.DutyCycle,
                Mode = s.Mode
            }).ToList(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(routine);

        _logger.LogInformation("Created internal routine: {RoutineName} (ID: {RoutineId})", routine.Name, routine.Id);

        return CreatedAtAction(nameof(GetById), new { id = routine.Id }, MapToDto(routine));
    }

    /// <summary>
    /// Update an existing internal routine
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Policy = PolicyNames.CommandCreate)]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateInternalRoutineDto dto)
    {
        var routine = await _repository.GetByIdAsync(id);

        if (routine == null)
            return NotFound(new { Message = $"Internal routine with ID {id} not found" });

        // Update fields if provided
        if (dto.Name != null)
            routine.Name = dto.Name;

        if (dto.Description != null)
            routine.Description = dto.Description;

        if (dto.Esp32Id != null)
            routine.Esp32Id = dto.Esp32Id;

        if (dto.Interval.HasValue)
            routine.Interval = dto.Interval.Value;

        if (dto.IsActive.HasValue)
            routine.IsActive = dto.IsActive.Value;

        if (dto.Steps != null)
        {
            // Validate OutputVariables (ActuatorCodes)
            var allActuators = await _actuatorRepository.GetAllAsync();
            foreach (var step in dto.Steps)
            {
                var actuator = allActuators.FirstOrDefault(a => a.Code == step.OutputVariable);
                if (actuator == null)
                {
                    return BadRequest(new
                    {
                        Message = $"Actuator with code '{step.OutputVariable}' not found"
                    });
                }
            }

            routine.Steps = dto.Steps.Select(s => new InternalRoutineStep
            {
                OutputVariable = s.OutputVariable,
                Power = s.Power,
                Duration = s.Duration,
                DutyCycle = s.DutyCycle,
                Mode = s.Mode
            }).ToList();
        }

        routine.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(routine);

        _logger.LogInformation("Updated internal routine: {RoutineName} (ID: {RoutineId})", routine.Name, routine.Id);

        return Ok(MapToDto(routine));
    }

    /// <summary>
    /// Delete an internal routine
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Policy = PolicyNames.ActuatorDelete)]
    public async Task<IActionResult> Delete(string id)
    {
        var routine = await _repository.GetByIdAsync(id);

        if (routine == null)
            return NotFound(new { Message = $"Internal routine with ID {id} not found" });

        await _repository.DeleteAsync(id);

        _logger.LogInformation("Deleted internal routine: {RoutineName} (ID: {RoutineId})", routine.Name, routine.Id);

        return Ok(new { Message = $"Internal routine '{routine.Name}' deleted successfully" });
    }

    private InternalRoutineDto MapToDto(InternalRoutine routine)
    {
        DateTime? nextExecution = null;

        if (routine.IsActive)
        {
            // Use deterministic calculation from InternalRoutineScheduler
            nextExecution = InternalRoutineScheduler.GetNextExecution(routine.Interval, "America/Bogota");
        }

        return new InternalRoutineDto
        {
            Id = routine.Id,
            Name = routine.Name,
            Description = routine.Description,
            Esp32Id = routine.Esp32Id,
            Interval = routine.Interval,
            Steps = routine.Steps.Select(s => new InternalRoutineStepDto
            {
                OutputVariable = s.OutputVariable,
                Power = s.Power,
                Duration = s.Duration,
                DutyCycle = s.DutyCycle,
                Mode = s.Mode
            }).ToList(),
            NextExecutionEstimate = nextExecution,
            IsActive = routine.IsActive,
            CreatedAt = routine.CreatedAt,
            UpdatedAt = routine.UpdatedAt
        };
    }

    private async Task<List<InternalRoutine>> GetInactiveRoutinesAsync()
    {
        // This would require a new repository method or filtering
        // For now, return empty list - can be extended later
        return new List<InternalRoutine>();
    }
}
