namespace SensorService.Application.DTOs.Aggregate;

/// <summary>
/// Respuesta con los datos agregados de una variable ambiental específica.
/// Incluye resumen estadístico, tendencia temporal y variabilidad (para vista diaria).
/// </summary>
public class EnvironmentalAggregateResponse
{
    /// <summary>Código de la variable ambiental (ej: TEMP, PH, EC).</summary>
    public string VariableCode { get; set; } = default!;
    /// <summary>Nombre descriptivo de la variable ambiental.</summary>
    public string VariableName { get; set; } = default!;
    /// <summary>Resumen estadístico general (mín, máx, promedio, conteo).</summary>
    public AggregateSummary Summary { get; set; } = default!;
    /// <summary>Serie temporal de promedios para gráficos de tendencia.</summary>
    public List<AggregateTrendPoint> Trend { get; set; } = new();
    /// <summary>Datos de variabilidad para gráficos boxplot (solo disponible en vista diaria).</summary>
    public List<AggregateVariabilityPoint>? Variability { get; set; }
}
