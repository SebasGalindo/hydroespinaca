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
            PhysicalMin = doc.PhysicalMin,
            PhysicalMax = doc.PhysicalMax,
            OptimalMin = doc.OptimalMin,
            OptimalMax = doc.OptimalMax,
            Type = doc.Type,
            RegulationType = doc.RegulationType,
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
            PhysicalMin = entity.PhysicalMin,
            PhysicalMax = entity.PhysicalMax,
            OptimalMin = entity.OptimalMin,
            OptimalMax = entity.OptimalMax,
            Type = entity.Type,
            RegulationType = entity.RegulationType,
            LastModified = entity.LastModified
        };
        document.SetId(entity.Id);
        return document;
    }
}
