namespace SensorService.Application.DTOs.Reading;

/// <summary>
/// DTO que representa una lectura individual de un sensor hidropónico.
/// Transporta el valor medido junto con la identificación del sensor y la variable asociada.
/// </summary>
public class ReadingDto
{
    /// <summary>Código único del sensor que generó la lectura.</summary>
    public string SensorCode { get; set; } = default!;
    /// <summary>Código de la variable ambiental medida (ej: TEMP, PH, EC).</summary>
    public string VariableCode { get; set; } = default!;
    /// <summary>Valor numérico de la lectura.</summary>
    public double Value { get; set; }
    /// <summary>Marca de tiempo en que se registró la lectura.</summary>
    public DateTime Timestamp { get; set; }
}
