using HydroEspinaca.Shared.Mongo.Interfaces;
using SensorService.Infrastructure.Persistence.Models;

namespace SensorService.Infrastructure.Persistence.Mappers;

public class VariableMapper : IEntityMapper<Variable, VariableDocument>
{
    public Variable ToEntity(VariableDocument doc)
    {
        var variable = new Variable
        {
            Name = doc.Name,
            Unit = doc.Unit,
            Description = doc.Description,
            MinValue = doc.MinValue,
            MaxValue = doc.MaxValue,
            Type = doc.Type,
            LastModified = doc.LastModified
        };
        variable.SetId(doc.Id);
        return variable;
    }

    public VariableDocument ToDocument(Variable entity)
    {
        var document = new VariableDocument
        {
            Name = entity.Name,
            Unit = entity.Unit,
            Description = entity.Description,
            MinValue = entity.MinValue,
            MaxValue = entity.MaxValue,
            Type = entity.Type,
            LastModified = entity.LastModified
        };
        document.SetId(entity.Id);
        return document;
    }
}
