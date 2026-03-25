using HydroEspinaca.Shared.Abstractions;

namespace BiService.Domain.Entities;

/// <summary>
/// Versión de configuración de costos unitarios para electricidad, agua y nutrientes.
/// Permite mantener un historial versionado de los precios aplicados en cada periodo.
/// </summary>
public class CostConfigVersion : IIdentifiableMutable
{
    /// <summary>Identificador único de la versión (asignado por MongoDB).</summary>
    public string Id { get; private set; } = string.Empty;

    /// <summary>Código ISO de la moneda (por defecto "COP").</summary>
    public string Currency { get; set; } = "COP";

    /// <summary>Costo por kilovatio-hora de electricidad.</summary>
    public decimal ElectricityCostPerKwh { get; set; }

    /// <summary>Costo por litro de agua.</summary>
    public decimal WaterCostPerLiter { get; set; }

    /// <summary>Costo por litro de solución nutritiva.</summary>
    public decimal NutrientCostPerLiter { get; set; }

    /// <summary>Fecha a partir de la cual esta versión entra en vigencia.</summary>
    public DateTime EffectiveFrom { get; set; }

    /// <summary>Fecha en la que esta versión dejó de estar vigente. <c>null</c> si sigue activa.</summary>
    public DateTime? EffectiveTo { get; set; }

    /// <summary>Indica si esta es la versión activa actualmente.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Fecha UTC de creación del registro.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Identificador del usuario que creó esta versión.</summary>
    public string CreatedByUserId { get; set; } = string.Empty;

    /// <inheritdoc />
    public void SetId(string id) => Id = id;
}
