namespace SensorService.Application.DTOs.Aggregate;

/// <summary>
/// Punto de variabilidad para gráficos de tipo boxplot.
/// Contiene los cuartiles y valores extremos de las lecturas en un período específico.
/// </summary>
public class AggregateVariabilityPoint
{
    /// <summary>Marca de tiempo del punto de variabilidad.</summary>
    public DateTime Timestamp { get; set; }
    /// <summary>Valor mínimo observado.</summary>
    public double Min { get; set; }
    /// <summary>Primer cuartil (percentil 25).</summary>
    public double Q1 { get; set; }
    /// <summary>Mediana (percentil 50).</summary>
    public double Median { get; set; }
    /// <summary>Tercer cuartil (percentil 75).</summary>
    public double Q3 { get; set; }
    /// <summary>Valor máximo observado.</summary>
    public double Max { get; set; }
    /// <summary>Cantidad de lecturas incluidas en el cálculo.</summary>
    public int Count { get; set; }
}
