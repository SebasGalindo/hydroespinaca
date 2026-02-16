using BiService.Domain.Enums;
using HydroEspinaca.Shared.Abstractions;

namespace BiService.Domain.Entities;

/// <summary>
/// Registro manual de consumo de un recurso (electricidad, agua o nutrientes).
/// Captura la cantidad consumida junto con una instantánea del costo unitario vigente
/// en el momento del registro para garantizar trazabilidad histórica.
/// </summary>
public class ManualConsumptionEntry : IIdentifiableMutable
{
    /// <summary>Identificador único del registro (asignado por MongoDB).</summary>
    public string Id { get; private set; } = string.Empty;

    /// <summary>Fecha en la que ocurrió el consumo.</summary>
    public DateTime Date { get; set; }

    /// <summary>Tipo de recurso consumido.</summary>
    public ConsumptionType Type { get; set; }

    /// <summary>Cantidad consumida en la unidad correspondiente al tipo.</summary>
    public decimal Amount { get; set; }

    /// <summary>Costo unitario vigente al momento del registro (snapshot).</summary>
    public decimal UnitCostSnapshot { get; set; }

    /// <summary>Moneda del costo unitario al momento del registro.</summary>
    public string CurrencySnapshot { get; set; } = "COP";

    /// <summary>ID de la versión de configuración de costos utilizada.</summary>
    public string CostConfigVersionId { get; set; } = string.Empty;

    /// <summary>Costo total calculado (<see cref="Amount"/> × <see cref="UnitCostSnapshot"/>).</summary>
    public decimal CostAmount { get; set; }

    /// <summary>Nota opcional descriptiva sobre el consumo.</summary>
    public string? Note { get; set; }

    /// <summary>Fecha UTC de creación del registro.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Identificador del usuario que registró el consumo.</summary>
    public string CreatedByUserId { get; set; } = string.Empty;

    /// <inheritdoc />
    public void SetId(string id) => Id = id;
}
