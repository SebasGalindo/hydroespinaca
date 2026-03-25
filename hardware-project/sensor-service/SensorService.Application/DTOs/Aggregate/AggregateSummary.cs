namespace SensorService.Application.DTOs.Aggregate;

/// <summary>
/// Resumen estadístico de un conjunto de datos agregados de sensores hidropónicos.
/// Contiene los valores mínimo, máximo, promedio y la cantidad de lecturas.
/// </summary>
public class AggregateSummary
{
    /// <summary>Valor mínimo registrado en el período.</summary>
    public double Min { get; set; }
    /// <summary>Valor máximo registrado en el período.</summary>
    public double Max { get; set; }
    /// <summary>Valor promedio calculado en el período.</summary>
    public double Avg { get; set; }
    /// <summary>Cantidad total de lecturas incluidas en el resumen.</summary>
    public int Count { get; set; }
}
