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

    // ──────────────────────────────────────────────
    //  Fuzzy Terms
    // ──────────────────────────────────────────────

    /// <summary>
    /// Gets all terms belonging to a specific variable.
    /// </summary>
    Task<List<FuzzyTermDto>> GetTermsByVariableAsync(
        string accessToken, string variableId, CancellationToken cancellationToken = default);

    // ──────────────────────────────────────────────
    //  Fuzzy Rules
    // ──────────────────────────────────────────────

    /// <summary>
    /// Gets all rules belonging to a fuzzy system.
    /// </summary>
    Task<List<FuzzyRuleDto>> GetRulesBySystemAsync(
        string accessToken, string systemId, CancellationToken cancellationToken = default);
}
