using HydroEspinaca.Shared.Abstractions;
using HydroEspinaca.Shared.Enums;

namespace SensorService.Domain.Entities;

public class Variable : IIdentifiableMutable
{
    public string Id { get; private set; } = default!;
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Unit { get; set; } = default!;
    public string Description { get; set; } = default!;
    
    // Rangos físicos - valores técnicamente posibles para el sensor
    public double PhysicalMin { get; set; }
    public double PhysicalMax { get; set; }
    
    // Rangos óptimos - valores ideales para el crecimiento de espinaca
    public double OptimalMin { get; set; }
    public double? OptimalMax { get; set; }
    
    public VariableTypes Type { get; set; }
    
    // Tipo de regulación - manual o automática
    public RegulationType? RegulationType { get; set; }
    
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    public void SetId(string id) => Id = id;
}