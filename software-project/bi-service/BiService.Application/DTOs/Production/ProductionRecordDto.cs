namespace BiService.Application.DTOs.Production;

/// <summary>
/// DTO de lectura de un registro de producción de cultivo.
/// </summary>
public class ProductionRecordDto
{
    /// <summary>Identificador único del registro.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Nombre del cultivo producido.</summary>
    public string CropName { get; set; } = string.Empty;

    /// <summary>Fecha de inicio del ciclo de cultivo.</summary>
    public DateTime StartDate { get; set; }

    /// <summary>Fecha de cosecha.</summary>
    public DateTime HarvestDate { get; set; }

    /// <summary>Kilogramos producidos en la cosecha.</summary>
    public decimal KilosProduced { get; set; }

    /// <summary>Precio de venta por kilogramo.</summary>
    public decimal PricePerKilo { get; set; }

    /// <summary>Código de moneda.</summary>
    public string Currency { get; set; } = "COP";

    /// <summary>Nota descriptiva opcional.</summary>
    public string? Note { get; set; }

    /// <summary>Fecha y hora de creación del registro.</summary>
    public DateTime CreatedAt { get; set; }
}
