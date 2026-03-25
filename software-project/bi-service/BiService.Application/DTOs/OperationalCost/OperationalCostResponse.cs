namespace BiService.Application.DTOs.OperationalCost;

/// <summary>
/// Respuesta con el desglose del costo operacional estimado por consumo eléctrico de actuadores.
/// </summary>
public class OperationalCostResponse
{
    /// <summary>Fecha de inicio del periodo calculado.</summary>
    public DateTime From { get; set; }

    /// <summary>Fecha de fin del periodo calculado.</summary>
    public DateTime To { get; set; }

    /// <summary>Código de moneda utilizada.</summary>
    public string Currency { get; set; } = "COP";

    /// <summary>Versiones de configuración de costos aplicadas en el periodo.</summary>
    public List<CostConfigPeriodUsed> CostConfigPeriodsUsed { get; set; } = new();

    /// <summary>Desglose de costo por cada actuador.</summary>
    public List<ActuatorOperationalCostItem> Actuators { get; set; } = new();

    /// <summary>Total de kilovatios-hora estimados consumidos.</summary>
    public decimal TotalEstimatedKwh { get; set; }

    /// <summary>Costo operacional total estimado.</summary>
    public decimal TotalOperationalCost { get; set; }
}

/// <summary>
/// Periodo de vigencia de una configuración de costos utilizada en el cálculo.
/// </summary>
public class CostConfigPeriodUsed
{
    /// <summary>Identificador de la versión de configuración.</summary>
    public string VersionId { get; set; } = string.Empty;

    /// <summary>Fecha de inicio de aplicación de esta configuración.</summary>
    public DateTime From { get; set; }

    /// <summary>Fecha de fin de aplicación de esta configuración.</summary>
    public DateTime To { get; set; }

    /// <summary>Costo por kWh vigente en este periodo.</summary>
    public decimal ElectricityCostPerKwh { get; set; }

    /// <summary>Código de moneda.</summary>
    public string Currency { get; set; } = "COP";
}

/// <summary>
/// Detalle de costo operacional de un actuador individual.
/// </summary>
public class ActuatorOperationalCostItem
{
    /// <summary>Código identificador del actuador.</summary>
    public string ActuatorCode { get; set; } = string.Empty;

    /// <summary>Potencia de consumo en vatios (W).</summary>
    public decimal PowerConsumptionWatts { get; set; }

    /// <summary>Duración total de funcionamiento en segundos.</summary>
    public double TotalDurationSeconds { get; set; }

    /// <summary>Duración total de funcionamiento en horas.</summary>
    public decimal TotalHours { get; set; }

    /// <summary>Kilovatios-hora estimados consumidos.</summary>
    public decimal EstimatedKwh { get; set; }

    /// <summary>Costo estimado para este actuador.</summary>
    public decimal EstimatedCost { get; set; }

    /// <summary>Cantidad de activaciones del actuador.</summary>
    public int ActivationCount { get; set; }
}
