namespace SensorService.Application.DTOs.Aggregate;

/// <summary>
/// Respuesta envolvente para los agregados ambientales del sistema hidropónico.
/// Coincide con la estructura esperada por el frontend: { variables: [...] }.
/// </summary>
public class EnvironmentalAggregatesResponse
{
    /// <summary>Lista de agregados ambientales, uno por cada variable monitoreada.</summary>
    public List<EnvironmentalAggregateResponse> Variables { get; set; } = new();
}
