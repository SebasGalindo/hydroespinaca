namespace BiService.Application.DTOs.CostConfig;

/// <summary>
/// Solicitud para actualizar una versión existente de configuración de costos.
/// </summary>
public class UpdateCostConfigVersionRequest
{
    /// <summary>Código de moneda.</summary>
    public string Currency { get; set; } = "COP";

    /// <summary>Costo por kilovatio-hora de electricidad.</summary>
    public decimal ElectricityCostPerKwh { get; set; }

    /// <summary>Costo por litro de agua.</summary>
    public decimal WaterCostPerLiter { get; set; }

    /// <summary>Costo por litro de solución nutritiva.</summary>
    public decimal NutrientCostPerLiter { get; set; }

    /// <summary>Fecha a partir de la cual entra en vigencia.</summary>
    public DateTime? EffectiveFrom { get; set; }

    /// <summary>Fecha hasta la cual está vigente (opcional).</summary>
    public DateTime? EffectiveTo { get; set; }
}
