using BiService.Domain.Enums;

namespace BiService.Application.DTOs.Consumption;

/// <summary>
/// Solicitud para crear un registro de consumo manual de recursos.
/// </summary>
public class CreateManualConsumptionEntryRequest
{
    /// <summary>Fecha de inicio del consumo (o fecha única).</summary>
    public DateTime DateFrom { get; set; }

    /// <summary>Fecha de fin del consumo (opcional, si se omite se usa DateFrom).</summary>
    public DateTime? DateTo { get; set; }

    /// <summary>Tipo de recurso consumido.</summary>
    public ConsumptionType Type { get; set; }

    /// <summary>Cantidad consumida en la unidad correspondiente.</summary>
    public decimal Amount { get; set; }

    /// <summary>Nota descriptiva opcional.</summary>
    public string? Note { get; set; }
}
