using HydroEspinaca.Shared.Mongo.Interfaces;
using SensorService.Infrastructure.Persistence.Models;

namespace SensorService.Infrastructure.Persistence.Mappers;

public class VariableMapper : IEntityMapper<Variable, VariableDocument>
{
    public Variable ToEntity(VariableDocument doc) => new()
    {
        Id = doc.Id,
        Name = doc.Name,
        Unit = doc.Unit,
        Description = doc.Description,
        MinValue = doc.MinValue,
        MaxValue = doc.MaxValue,
        Type = doc.Type,
        LastModified = doc.LastModified
    };

    public VariableDocument ToDocument(Variable entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Unit = entity.Unit,
        Description = entity.Description,
        MinValue = entity.MinValue,
        MaxValue = entity.MaxValue,
        Type = entity.Type,
        LastModified = entity.LastModified
    };
}
