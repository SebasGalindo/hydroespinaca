namespace BffService.Domain.DTOs.Bi;

/// <summary>
/// BFF request for profitability calculation.
/// The BFF orchestrates: gets production record, calls actuator analytics, 
/// combines with PowerConsumptionWatts, then calls bi-service for calculation.
/// </summary>
public class CalculateProfitabilityBffRequest
{
    public string ProductionRecordId { get; set; } = string.Empty;
}
