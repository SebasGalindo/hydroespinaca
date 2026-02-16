namespace BffService.Domain.DTOs.Bi;

/// <summary>
/// Request for calculating operational cost.
/// The BFF orchestrates: calls actuator-service for data, 
/// then sends combined data to bi-service for calculation.
/// </summary>
public class CalculateOperationalCostBffRequest
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
}
