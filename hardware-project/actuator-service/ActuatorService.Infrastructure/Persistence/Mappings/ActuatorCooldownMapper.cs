using ActuatorService.Domain.Entities;
using ActuatorService.Infrastructure.Persistence.Models;
using HydroEspinaca.Shared.Mongo.Interfaces;

namespace ActuatorService.Infrastructure.Persistence.Mappings;

public class ActuatorCooldownMapper : IEntityMapper<ActuatorCooldown, ActuatorCooldownDocument>
{
    public ActuatorCooldownDocument ToDocument(ActuatorCooldown entity)
    {
        return new ActuatorCooldownDocument
        {
            Id = entity.Id,
            ActuatorId = entity.ActuatorId,
            Reason = entity.Reason,
            StartedAt = entity.StartedAt,
            ExpiresAt = entity.ExpiresAt,
            IsActive = entity.IsActive
        };
    }

    public ActuatorCooldown ToEntity(ActuatorCooldownDocument document)
    {
        return new ActuatorCooldown
        {
            Id = document.Id,
            ActuatorId = document.ActuatorId,
            Reason = document.Reason,
            StartedAt = document.StartedAt,
            ExpiresAt = document.ExpiresAt,
            IsActive = document.IsActive
        };
    }
}
