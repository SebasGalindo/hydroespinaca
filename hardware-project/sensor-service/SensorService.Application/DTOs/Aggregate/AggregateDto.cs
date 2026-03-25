namespace SensorService.Application.DTOs.Aggregate;

/// <summary>
/// DTO que representa un registro de datos agregados de un sensor y variable específicos.
/// Contiene las estadísticas calculadas (promedio, mínimo, máximo) para un período de tiempo.
/// </summary>
public class AggregateDto
{
    /// <summary>Código del sensor al que pertenecen los datos agregados.</summary>
    public string SensorCode { get; set; } = default!;
    /// <summary>Código de la variable ambiental agregada.</summary>
    public string VariableCode { get; set; } = default!;
    /// <summary>Promedio de los valores en el período.</summary>
    public double Avg { get; set; }
    /// <summary>Valor mínimo en el período.</summary>
    public double Min { get; set; }
    /// <summary>Valor máximo en el período.</summary>
    public double Max { get; set; }
    /// <summary>Cantidad de lecturas incluidas en la agregación.</summary>
    public int Count { get; set; }
    /// <summary>Marca de tiempo que identifica el período de agregación.</summary>
    public DateTime Timestamp { get; set; }
}