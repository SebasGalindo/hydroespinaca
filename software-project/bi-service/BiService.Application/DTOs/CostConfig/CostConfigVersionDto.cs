namespace BiService.Application.DTOs.CostConfig;

/// <summary>
/// DTO de lectura de una versión de configuración de costos.
/// </summary>
public class CostConfigVersionDto
{
    /// <summary>Identificador único de la versión.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Código de moneda.</summary>
    public string Currency { get; set; } = "COP";

    /// <summary>Costo por kilovatio-hora de electricidad.</summary>
    public decimal ElectricityCostPerKwh { get; set; }

    /// <summary>Costo por litro de agua.</summary>
    public decimal WaterCostPerLiter { get; set; }

    /// <summary>Costo por litro de solución nutritiva.</summary>
    public decimal NutrientCostPerLiter { get; set; }

    /// <summary>Fecha de inicio de vigencia.</summary>
    public DateTime EffectiveFrom { get; set; }

    /// <summary>Fecha de fin de vigencia; <c>null</c> si aún está activa.</summary>
    public DateTime? EffectiveTo { get; set; }

    /// <summary>Indica si esta versión es la actualmente vigente.</summary>
    public bool IsActive { get; set; }

    /// <summary>Fecha y hora de creación del registro.</summary>
    public DateTime CreatedAt { get; set; }
}
