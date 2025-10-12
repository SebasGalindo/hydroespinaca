using HydroEspinaca.Shared.Abstractions;

namespace SensorService.Domain.Entities;
public class Reading : IIdentifiableMutable
{
    public string Id { get; private set; } = default!;
    public string SensorCode { get; set; } = default!;
    public string VariableCode { get; set; } = default!;
    public double Value { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public void SetId(string id) => Id = id;

}