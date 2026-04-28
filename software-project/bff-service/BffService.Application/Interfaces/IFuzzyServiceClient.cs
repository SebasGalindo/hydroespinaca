using BffService.Domain.DTOs;
using BffService.Domain.DTOs.Fuzzy;

namespace BffService.Application.Interfaces;

/// <summary>
/// Client for communicating with the fuzzy-service microservice.
/// Handles fuzzy systems, variables, terms, rules, and advanced operations
/// (activate, clone, export, import, simulate).
/// </summary>
public interface IFuzzyServiceClient
{
    // ──────────────────────────────────────────────
    //  Legacy (sin token – endpoint público cacheado)
    // ──────────────────────────────────────────────

    /// <summary>
    /// Gets a summary list of all fuzzy rules with their names and descriptions.
    /// Used by SystemStatusController (cached 24h).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of fuzzy rule summaries</returns>
    Task<List<FuzzyRuleSummaryDto>> GetRulesSummaryAsync(CancellationToken cancellationToken = default);

    // ──────────────────────────────────────────────
    //  Fuzzy Systems
    // ──────────────────────────────────────────────

    /// <summary>
    /// Lists all fuzzy systems with optional filters.
    /// </summary>
    Task<List<FuzzySystemDto>> GetSystemsAsync(
        string accessToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a single fuzzy system by ID.
    /// </summary>
    Task<FuzzySystemDto?> GetSystemByIdAsync(
        string accessToken, string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new fuzzy system with the given name and optional configuration.
    /// </summary>
    Task<FuzzySystemDto> CreateSystemAsync(
        string accessToken, CreateFuzzySystemRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing fuzzy system's properties.
    /// </summary>
    Task<FuzzySystemDto> UpdateSystemAsync(
        string accessToken, string id, UpdateFuzzySystemRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Changes the status of a fuzzy system (DRAFT, ACTIVE, INACTIVE, TESTING).
    /// </summary>
    Task<FuzzySystemDto> UpdateSystemStatusAsync(
        string accessToken, string id, UpdateFuzzySystemStatusRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Activates a fuzzy system exclusively (deactivates all others).
    /// </summary>
    Task<FuzzySystemDto> ActivateSystemAsync(
        string accessToken, string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deep-clones a fuzzy system with all its variables, terms and rules.
    /// </summary>
    Task<FuzzySystemDto> CloneSystemAsync(
        string accessToken, string id, CloneFuzzySystemRequest? request = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports a fuzzy system to a portable JSON format.
    /// </summary>
    Task<Dictionary<string, object?>> ExportSystemAsync(
        string accessToken, string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports a fuzzy system from a portable JSON format.
    /// </summary>
    Task<FuzzySystemDto> ImportSystemAsync(
        string accessToken, Dictionary<string, object?> exportData,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Simulates a fuzzy evaluation without persisting results.
    /// </summary>
    Task<SimulateFuzzySystemResponse> SimulateSystemAsync(
        string accessToken, string id, SimulateFuzzySystemRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a fuzzy system by ID.
    /// </summary>
    Task DeleteSystemAsync(
        string accessToken, string id, CancellationToken cancellationToken = default);

    // ──────────────────────────────────────────────
    //  Fuzzy Variables
    // ──────────────────────────────────────────────

    /// <summary>
    /// Gets all variables belonging to a fuzzy system.
    /// </summary>
    Task<List<FuzzyVariableDto>> GetVariablesBySystemAsync(
        string accessToken, string systemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new fuzzy variable and associates it with a system.
    /// </summary>
    Task<FuzzyVariableDto> CreateVariableAsync(
        string accessToken, CreateFuzzyVariableRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing fuzzy variable's properties.
    /// </summary>
    Task<FuzzyVariableDto> UpdateVariableAsync(
        string accessToken, string id, UpdateFuzzyVariableRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a fuzzy variable by ID.
    /// </summary>
    Task DeleteVariableAsync(
        string accessToken, string id, CancellationToken cancellationToken = default);

    // ──────────────────────────────────────────────
    //  Fuzzy Terms
    // ──────────────────────────────────────────────

    /// <summary>
    /// Gets all terms belonging to a specific variable.
    /// </summary>
    Task<List<FuzzyTermDto>> GetTermsByVariableAsync(
        string accessToken, string variableId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new fuzzy term for a variable.
    /// </summary>
    Task<FuzzyTermDto> CreateTermAsync(
        string accessToken, CreateFuzzyTermRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing fuzzy term's label or membership function.
    /// </summary>
    Task<FuzzyTermDto> UpdateTermAsync(
        string accessToken, string id, UpdateFuzzyTermRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a fuzzy term by ID.
    /// </summary>
    Task DeleteTermAsync(
        string accessToken, string id, CancellationToken cancellationToken = default);

    // ──────────────────────────────────────────────
    //  Fuzzy Rules
    // ──────────────────────────────────────────────

    /// <summary>
    /// Gets all rules belonging to a fuzzy system.
    /// </summary>
    Task<List<FuzzyRuleDto>> GetRulesBySystemAsync(
        string accessToken, string systemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new fuzzy rule for a system.
    /// </summary>
    Task<FuzzyRuleDto> CreateRuleAsync(
        string accessToken, CreateFuzzyRuleRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing fuzzy rule's conditions, connectors or consequents.
    /// </summary>
    Task<FuzzyRuleDto> UpdateRuleAsync(
        string accessToken, string id, UpdateFuzzyRuleRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a fuzzy rule by ID.
    /// </summary>
    Task DeleteRuleAsync(
        string accessToken, string id, CancellationToken cancellationToken = default);

    // ──────────────────────────────────────────────
    //  Fuzzy Evaluations (RF-F07: history / recent / stats)
    // ──────────────────────────────────────────────

    /// <summary>
    /// Lists persisted fuzzy evaluations with optional filters and pagination.
    /// </summary>
    Task<FuzzyEvaluationsListResponseDto> GetEvaluationsAsync(
        string accessToken,
        string? systemId,
        DateTime? startDate,
        DateTime? endDate,
        int page,
        int pageSize,
        string sortBy,
        string sortOrder,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists evaluations performed in the last <paramref name="hours"/> hours
    /// (24 by default, capped at 168 = 7 days by the backend).
    /// </summary>
    Task<FuzzyEvaluationsListResponseDto> GetRecentEvaluationsAsync(
        string accessToken,
        int hours,
        string? systemId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns aggregate statistics (totals per system, per day, daily average)
    /// over a window of <paramref name="days"/> days.
    /// </summary>
    Task<FuzzyEvaluationStatsDto> GetEvaluationStatsAsync(
        string accessToken,
        string? systemId,
        int days,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists evaluations for a specific system, with optional date range.
    /// Uses the fuzzy-service /system/{id} endpoint which correctly applies
    /// date filters together with the system filter.
    /// </summary>
    Task<FuzzyEvaluationsListResponseDto> GetEvaluationsBySystemAsync(
        string accessToken,
        string systemId,
        DateTime? startDate,
        DateTime? endDate,
        int page,
        int pageSize,
        string sortOrder,
        CancellationToken cancellationToken = default);
}
