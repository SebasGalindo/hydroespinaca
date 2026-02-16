namespace BiService.Application.DTOs.OperationalCost;

/// <summary>
/// Datos de entrada con la duración y potencia de un actuador para el cálculo de costos operacionales.
/// </summary>
public class ActuatorDurationInput
{
    /// <summary>Código identificador del actuador.</summary>
    public string ActuatorCode { get; set; } = string.Empty;

    /// <summary>Potencia de consumo del actuador en vatios (W).</summary>
    public decimal PowerConsumptionWatts { get; set; }

    /// <summary>Duración total de funcionamiento en segundos.</summary>
    public double TotalDurationSeconds { get; set; }

    /// <summary>Cantidad de veces que se activó el actuador.</summary>
    public int ActivationCount { get; set; }
}
