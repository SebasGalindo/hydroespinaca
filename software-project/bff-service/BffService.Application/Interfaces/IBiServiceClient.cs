using BffService.Domain.DTOs.Bi;

namespace BffService.Application.Interfaces;

/// <summary>
/// Client for communicating with the bi-service microservice.
/// Handles cost configuration, manual consumption entries, and BI summaries.
/// </summary>
public interface IBiServiceClient
{
    /// <summary>
    /// Gets the current active cost configuration version
    /// </summary>
    Task<CostConfigVersionDto?> GetCurrentCostConfigAsync(
        string accessToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets cost configuration versions filtered by optional date range
    /// </summary>
    Task<List<CostConfigVersionDto>> GetCostConfigVersionsAsync(
        string accessToken, DateTime? from = null, DateTime? to = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new cost configuration version
    /// </summary>
    Task<CostConfigVersionDto> CreateCostConfigVersionAsync(
        string accessToken, CreateCostConfigVersionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing cost configuration version
    /// </summary>
    Task<CostConfigVersionDto> UpdateCostConfigVersionAsync(
        string accessToken, string id, UpdateCostConfigVersionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a cost configuration version
    /// </summary>
    Task DeleteCostConfigVersionAsync(
        string accessToken, string id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new manual consumption entry
    /// </summary>
    Task<ManualConsumptionEntryDto> CreateConsumptionEntryAsync(
        string accessToken, CreateManualConsumptionEntryRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets consumption entries filtered by date range and optional type
    /// </summary>
    Task<List<ManualConsumptionEntryDto>> GetConsumptionEntriesAsync(
        string accessToken, DateTime from, DateTime to, string? type = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a consumption cost summary for the given date range
    /// </summary>
    Task<BiSummaryDto> GetConsumptionSummaryAsync(
        string accessToken, DateTime from, DateTime to,
        CancellationToken cancellationToken = default);

    // ──────────────────────────────────────────────
    //  Consumption Entry - Delete
    // ──────────────────────────────────────────────

    /// <summary>
    /// Deletes a manual consumption entry by ID
    /// </summary>
    Task DeleteConsumptionEntryAsync(
        string accessToken, string id,
        CancellationToken cancellationToken = default);

    // ──────────────────────────────────────────────
    //  Production Records
    // ──────────────────────────────────────────────

    /// <summary>
    /// Creates a new production record
    /// </summary>
    Task<ProductionRecordDto> CreateProductionRecordAsync(
        string accessToken, CreateProductionRecordRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all production records
    /// </summary>
    Task<List<ProductionRecordDto>> GetProductionRecordsAsync(
        string accessToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a production record by ID
    /// </summary>
    Task<ProductionRecordDto?> GetProductionRecordByIdAsync(
        string accessToken, string id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a production record by ID
    /// </summary>
    Task DeleteProductionRecordAsync(
        string accessToken, string id,
        CancellationToken cancellationToken = default);

    // ──────────────────────────────────────────────
    //  Operational Cost (bi-service calculation)
    // ──────────────────────────────────────────────

    /// <summary>
    /// Sends pre-combined actuator data to bi-service for operational cost calculation.
    /// The BFF is responsible for combining actuator PowerConsumptionWatts with analytics data.
    /// </summary>
    Task<OperationalCostResponse> CalculateOperationalCostAsync(
        string accessToken, CalculateOperationalCostServiceRequest request,
        CancellationToken cancellationToken = default);

    // ──────────────────────────────────────────────
    //  Profitability (bi-service calculation)
    // ──────────────────────────────────────────────

    /// <summary>
    /// Sends pre-combined actuator data to bi-service for profitability calculation.
    /// The BFF is responsible for combining actuator PowerConsumptionWatts with analytics data.
    /// </summary>
    Task<ProfitabilityResponse> CalculateProfitabilityAsync(
        string accessToken, CalculateProfitabilityServiceRequest request,
        CancellationToken cancellationToken = default);
}
