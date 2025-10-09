using ActuatorService.Application.DTOs;
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
    private readonly IControlOutputRepository _controlOutputRepository;
    private readonly ILogger<InternalRoutinesController> _logger;

    public InternalRoutinesController(
        IInternalRoutineRepository repository,
        IControlOutputRepository controlOutputRepository,
        ILogger<InternalRoutinesController> logger)
    {
        _repository = repository;
        _controlOutputRepository = controlOutputRepository;
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
        // Validate that all OutputVariables exist
        foreach (var step in dto.Steps)
        {
            var controlOutput = await _controlOutputRepository.GetByIdAsync(step.OutputVariable);
            if (controlOutput == null)
            {
                return BadRequest(new
                {
                    Message = $"OutputVariable '{step.OutputVariable}' not found in control_outputs collection"
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
            StartTime = dto.StartTime,
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

        if (dto.StartTime.HasValue)
            routine.StartTime = dto.StartTime.Value;

        if (dto.IsActive.HasValue)
            routine.IsActive = dto.IsActive.Value;

        if (dto.Steps != null)
        {
            // Validate OutputVariables
            foreach (var step in dto.Steps)
            {
                var controlOutput = await _controlOutputRepository.GetByIdAsync(step.OutputVariable);
                if (controlOutput == null)
                {
                    return BadRequest(new
                    {
                        Message = $"OutputVariable '{step.OutputVariable}' not found in control_outputs collection"
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
        var now = DateTime.UtcNow;
        DateTime? nextExecution = null;

        if (routine.IsActive)
        {
            if (routine.LastExecutedAt == null)
            {
                // First execution: calculate from today's start time
                var todayStart = now.Date + routine.StartTime;
                nextExecution = todayStart > now ? todayStart : todayStart.Add(routine.Interval);
            }
            else
            {
                // Next execution based on last execution + interval
                nextExecution = routine.LastExecutedAt.Value.Add(routine.Interval);
            }
        }

        return new InternalRoutineDto
        {
            Id = routine.Id,
            Name = routine.Name,
            Description = routine.Description,
            Esp32Id = routine.Esp32Id,
            Interval = routine.Interval,
            StartTime = routine.StartTime,
            Steps = routine.Steps.Select(s => new InternalRoutineStepDto
            {
                OutputVariable = s.OutputVariable,
                Power = s.Power,
                Duration = s.Duration,
                DutyCycle = s.DutyCycle,
                Mode = s.Mode
            }).ToList(),
            LastExecutedAt = routine.LastExecutedAt,
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
