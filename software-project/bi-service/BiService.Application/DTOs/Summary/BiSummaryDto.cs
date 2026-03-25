namespace BiService.Application.DTOs.Summary;

/// <summary>
/// Resumen consolidado de consumo y costos de recursos para un periodo dado.
/// </summary>
public class BiSummaryDto
{
    /// <summary>Fecha de inicio del periodo.</summary>
    public DateTime From { get; set; }

    /// <summary>Fecha de fin del periodo.</summary>
    public DateTime To { get; set; }

    /// <summary>Código de moneda utilizada.</summary>
    public string Currency { get; set; } = "COP";

    /// <summary>Total de electricidad consumida en kilovatios-hora.</summary>
    public decimal TotalElectricityKwh { get; set; }

    /// <summary>Total de agua consumida en litros.</summary>
    public decimal TotalWaterLiters { get; set; }

    /// <summary>Total de solución nutritiva consumida en litros.</summary>
    public decimal TotalNutrientLiters { get; set; }

    /// <summary>Costo total por electricidad.</summary>
    public decimal CostElectricity { get; set; }

    /// <summary>Costo total por agua.</summary>
    public decimal CostWater { get; set; }

    /// <summary>Costo total por solución nutritiva.</summary>
    public decimal CostNutrients { get; set; }

    /// <summary>Costo total consolidado de todos los recursos.</summary>
    public decimal CostTotal { get; set; }
}
