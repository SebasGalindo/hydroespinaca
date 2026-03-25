namespace SensorService.Application.DTOs.Aggregate;

/// <summary>
/// Punto de datos de tendencia para gráficos de series temporales.
/// Representa el promedio de una variable ambiental en un momento específico.
/// </summary>
public class AggregateTrendPoint
{
    /// <summary>Marca de tiempo del punto de tendencia.</summary>
    public DateTime Timestamp { get; set; }
    /// <summary>Valor promedio de la variable en este punto temporal.</summary>
    public double Avg { get; set; }
}
