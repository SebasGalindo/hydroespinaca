using HydroEspinaca.Shared.Mongo.Interfaces;
using SensorService.Domain.Entities;
using SensorService.Infrastructure.Persistence.Models;

namespace SensorService.Infrastructure.Persistence.Mappers;

/// <summary>
/// Mapper que convierte entre la entidad de dominio <see cref="Variable"/> y el documento MongoDB <see cref="VariableDocument"/>.
/// </summary>
public class VariableMapper : IEntityMapper<Variable, VariableDocument>
{
    /// <summary>
    /// Convierte un documento MongoDB de variable a su entidad de dominio correspondiente.
    /// </summary>
    /// <param name="doc">El documento MongoDB de variable.</param>
    /// <returns>La entidad de dominio <see cref="Variable"/>.</returns>
    public Variable ToEntity(VariableDocument doc)
    {
        var variable = new Variable
        {
            Code = doc.Code,
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

    /// <summary>
    /// Convierte una entidad de dominio de variable a su documento MongoDB correspondiente.
    /// </summary>
    /// <param name="entity">La entidad de dominio <see cref="Variable"/>.</param>
    /// <returns>El documento MongoDB <see cref="VariableDocument"/>.</returns>
    public VariableDocument ToDocument(Variable entity)
    {
        var document = new VariableDocument
        {
            Code = entity.Code,
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
