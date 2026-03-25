using ChatbotService.Domain.Models.Fuzzy;

namespace ChatbotService.Domain.Interfaces;

public interface IFuzzyEntityHydratorService
{
    Task<HydratedFuzzySystem?> GetHydratedSystemAsync(string systemId, CancellationToken ct);
    Task<HydratedFuzzyVariable?> GetHydratedVariableAsync(string variableId, CancellationToken ct);
    Task<HydratedFuzzyRule?> GetHydratedRuleAsync(string ruleId, CancellationToken ct);
}
