namespace BffService.Domain.DTOs.Bi;

/// <summary>
/// DTO for manual consumption entry data forwarded from the BI service.
/// </summary>
public class ManualConsumptionEntryDto
{
    public string Id { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public int Type { get; set; }
    public decimal Amount { get; set; }
    public decimal UnitCostSnapshot { get; set; }
    public string CurrencySnapshot { get; set; } = "COP";
    public string CostConfigVersionId { get; set; } = string.Empty;
    public decimal CostAmount { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
}
