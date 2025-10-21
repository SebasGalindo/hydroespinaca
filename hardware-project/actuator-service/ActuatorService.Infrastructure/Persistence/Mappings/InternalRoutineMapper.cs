using ActuatorService.Domain.Entities;
using ActuatorService.Infrastructure.Persistence.Models;
using HydroEspinaca.Shared.Mongo.Interfaces;

namespace ActuatorService.Infrastructure.Persistence.Mappings;

public class InternalRoutineMapper : IEntityMapper<InternalRoutine, InternalRoutineDocument>
{
    public InternalRoutineDocument ToDocument(InternalRoutine entity)
    {
        return new InternalRoutineDocument
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            Esp32Id = entity.Esp32Id,
            Interval = entity.Interval,
            Steps = entity.Steps.Select(step => new InternalRoutineStepDocument
            {
                OutputVariable = step.OutputVariable,
                Power = step.Power,
                Duration = step.Duration,
                DutyCycle = step.DutyCycle,
                Mode = step.Mode
            }).ToList(),
            IsActive = entity.IsActive,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    public InternalRoutine ToEntity(InternalRoutineDocument document)
    {
        return new InternalRoutine
        {
            Id = document.Id,
            Name = document.Name,
            Description = document.Description,
            Esp32Id = document.Esp32Id,
            Interval = document.Interval,
            Steps = document.Steps.Select(step => new InternalRoutineStep
            {
                OutputVariable = step.OutputVariable,
                Power = step.Power,
                Duration = step.Duration,
                DutyCycle = step.DutyCycle,
                Mode = step.Mode
            }).ToList(),
            IsActive = document.IsActive,
            CreatedAt = document.CreatedAt,
            UpdatedAt = document.UpdatedAt
        };
    }
}
