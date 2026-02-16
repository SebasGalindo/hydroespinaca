using BffService.Domain.DTOs.Bi;

namespace BffService.Application.Interfaces;

/// <summary>
/// Orchestration service for BI operations that require data from multiple microservices.
/// The BFF is responsible for combining actuator data (from actuator-service) 
/// with cost calculations (from bi-service).
/// </summary>
public interface IBiOrchestrationService
{
    /// <summary>
    /// Orchestrates operational cost calculation:
    /// 1. GET actuators from actuator-service (to get PowerConsumptionWatts)
    /// 2. POST analytics to actuator-service (to get TotalDurationSeconds per actuator)
    /// 3. Combine data and POST to bi-service for cost calculation
    /// </summary>
    Task<OperationalCostResponse> CalculateOperationalCostAsync(
        CalculateOperationalCostBffRequest request,
        string accessToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Orchestrates profitability calculation:
    /// 1. GET production record from bi-service (to get date range)
    /// 2. GET actuators from actuator-service (to get PowerConsumptionWatts)
    /// 3. POST analytics to actuator-service (to get TotalDurationSeconds for production date range)
    /// 4. Combine data and POST to bi-service for profitability calculation
    /// </summary>
    Task<ProfitabilityResponse> CalculateProfitabilityAsync(
        CalculateProfitabilityBffRequest request,
        string accessToken,
        CancellationToken cancellationToken = default);
}
