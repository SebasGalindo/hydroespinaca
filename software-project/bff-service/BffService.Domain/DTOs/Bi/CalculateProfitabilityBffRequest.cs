namespace BffService.Domain.DTOs.Bi;

/// <summary>
/// BFF request for profitability calculation.
/// The BFF orchestrates: gets production record, calls actuator analytics, 
/// combines with PowerConsumptionWatts, then calls bi-service for calculation.
/// </summary>
public class CalculateProfitabilityBffRequest
{
    public string ProductionRecordId { get; set; } = string.Empty;

    /// <summary>
    /// Costo de inversión inicial en infraestructura y hardware (opcional).
    /// Cuando se provee, la respuesta incluye el ROI real del ciclo.
    /// </summary>
    public decimal? InitialInvestmentCost { get; set; }

    /// <summary>
    /// When true (default), the BFF fetches actuator durations and adds them to the calculation.
    /// Set to false if electricity was already entered manually to avoid double-counting.
    /// </summary>
    public bool IncludeAutomaticEnergyCalculation { get; set; } = true;
}
