namespace BffService.Domain.DTOs.Bi;

/// <summary>
/// Request DTO for creating a manual consumption entry via the BFF.
/// </summary>
public class CreateManualConsumptionEntryRequest
{
    public DateTime Date { get; set; }
    public int Type { get; set; }
    public decimal Amount { get; set; }
    public string? Note { get; set; }
}
