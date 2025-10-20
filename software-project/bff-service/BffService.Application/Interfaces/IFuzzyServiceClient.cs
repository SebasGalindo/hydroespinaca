using BffService.Domain.DTOs;

namespace BffService.Application.Interfaces;

/// <summary>
/// Client for communicating with the fuzzy-service microservice
/// </summary>
public interface IFuzzyServiceClient
{
    /// <summary>
    /// Gets a summary list of all fuzzy rules with their names and descriptions
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of fuzzy rule summaries</returns>
    Task<List<FuzzyRuleSummaryDto>> GetRulesSummaryAsync(CancellationToken cancellationToken = default);
}
