namespace BffService.Domain.DTOs.Bi;

/// <summary>
/// Internal request sent from BFF to bi-service POST /api/bi/profitability/calculate.
/// Mirrors BiService.Application.DTOs.Profitability.CalculateProfitabilityRequest.
/// </summary>
public class CalculateProfitabilityServiceRequest
{
    public string ProductionRecordId { get; set; } = string.Empty;
    public List<ActuatorDurationInput> ActuatorDurations { get; set; } = new();
    public decimal? InitialInvestmentCost { get; set; }
}
