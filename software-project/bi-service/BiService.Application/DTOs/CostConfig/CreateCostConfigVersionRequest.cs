namespace BiService.Application.DTOs.CostConfig;

/// <summary>
/// Solicitud para crear una nueva versión de configuración de costos unitarios.
/// </summary>
public class CreateCostConfigVersionRequest
{
    /// <summary>Código de moneda (por defecto COP).</summary>
    public string Currency { get; set; } = "COP";

    /// <summary>Costo por kilovatio-hora de electricidad.</summary>
    public decimal ElectricityCostPerKwh { get; set; }

    /// <summary>Costo por litro de agua.</summary>
    public decimal WaterCostPerLiter { get; set; }

    /// <summary>Costo por litro de solución nutritiva.</summary>
    public decimal NutrientCostPerLiter { get; set; }

    /// <summary>Fecha a partir de la cual entra en vigencia esta configuración.</summary>
    public DateTime? EffectiveFrom { get; set; }
}
