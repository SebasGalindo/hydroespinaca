namespace SensorService.Application.DTOs.Aggregate;

/// <summary>
/// Punto de datos agregado que contiene estadísticas completas para un período específico.
/// Utilizado para la visualización detallada de datos ambientales del sistema hidropónico.
/// </summary>
public class AggregateDataPoint
{
    /// <summary>Marca de tiempo del punto de datos.</summary>
    public DateTime Timestamp { get; set; }
    /// <summary>Promedio de los valores en el período.</summary>
    public double Avg { get; set; }
    /// <summary>Valor mínimo en el período.</summary>
    public double Min { get; set; }
    /// <summary>Valor máximo en el período.</summary>
    public double Max { get; set; }
    /// <summary>Cantidad de lecturas en el período.</summary>
    public int Count { get; set; }
}
