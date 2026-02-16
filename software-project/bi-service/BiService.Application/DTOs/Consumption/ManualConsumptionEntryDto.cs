using BiService.Domain.Enums;

namespace BiService.Application.DTOs.Consumption;

/// <summary>
/// DTO de lectura de un registro de consumo manual con su costo asociado.
/// </summary>
public class ManualConsumptionEntryDto
{
    /// <summary>Identificador único del registro.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Fecha del consumo.</summary>
    public DateTime Date { get; set; }

    /// <summary>Tipo de recurso consumido.</summary>
    public ConsumptionType Type { get; set; }

    /// <summary>Cantidad consumida en la unidad correspondiente.</summary>
    public decimal Amount { get; set; }

    /// <summary>Costo unitario vigente al momento del registro.</summary>
    public decimal UnitCostSnapshot { get; set; }

    /// <summary>Moneda vigente al momento del registro.</summary>
    public string CurrencySnapshot { get; set; } = "COP";

    /// <summary>Identificador de la versión de configuración de costos utilizada.</summary>
    public string CostConfigVersionId { get; set; } = string.Empty;

    /// <summary>Monto total del costo calculado (cantidad × costo unitario).</summary>
    public decimal CostAmount { get; set; }

    /// <summary>Nota descriptiva opcional.</summary>
    public string? Note { get; set; }

    /// <summary>Fecha y hora de creación del registro.</summary>
    public DateTime CreatedAt { get; set; }
}
