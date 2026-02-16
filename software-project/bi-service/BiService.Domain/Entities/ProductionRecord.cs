using HydroEspinaca.Shared.Abstractions;

namespace BiService.Domain.Entities;

/// <summary>
/// Registro de producción de un ciclo de cultivo. Almacena datos de cosecha,
/// cantidad producida y precio de venta para el cálculo de rentabilidad.
/// </summary>
public class ProductionRecord : IIdentifiableMutable
{
    /// <summary>Identificador único del registro (asignado por MongoDB).</summary>
    public string Id { get; private set; } = string.Empty;

    /// <summary>Nombre del cultivo (ej. "Espinaca", "Lechuga").</summary>
    public string CropName { get; set; } = string.Empty;

    /// <summary>Fecha de inicio del ciclo de cultivo.</summary>
    public DateTime StartDate { get; set; }

    /// <summary>Fecha de cosecha / fin del ciclo.</summary>
    public DateTime HarvestDate { get; set; }

    /// <summary>Kilogramos totales producidos en la cosecha.</summary>
    public decimal KilosProduced { get; set; }

    /// <summary>Precio de venta por kilogramo.</summary>
    public decimal PricePerKilo { get; set; }

    /// <summary>Código ISO de la moneda de venta (por defecto "COP").</summary>
    public string Currency { get; set; } = "COP";

    /// <summary>Nota opcional sobre la producción.</summary>
    public string? Note { get; set; }

    /// <summary>Fecha UTC de creación del registro.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Identificador del usuario que creó el registro.</summary>
    public string CreatedByUserId { get; set; } = string.Empty;

    /// <inheritdoc />
    public void SetId(string id) => Id = id;
}
