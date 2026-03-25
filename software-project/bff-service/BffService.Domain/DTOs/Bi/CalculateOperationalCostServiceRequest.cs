namespace BffService.Domain.DTOs.Bi;

/// <summary>
/// Internal request sent from BFF to bi-service POST /api/bi/operational-cost/calculate.
/// Mirrors BiService.Application.DTOs.OperationalCost.CalculateOperationalCostRequest.
/// </summary>
public class CalculateOperationalCostServiceRequest
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public List<ActuatorDurationInput> ActuatorDurations { get; set; } = new();
}
