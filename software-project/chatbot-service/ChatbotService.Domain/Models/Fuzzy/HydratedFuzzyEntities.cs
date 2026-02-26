using System.Collections.Generic;

namespace ChatbotService.Domain.Models.Fuzzy;

public record HydratedFuzzySystem(
    string Id,
    string Name,
    string Description,
    string Status,
    string DefuzzificationMethod,
    List<string> InputVariables,
    List<string> OutputVariables,
    List<string> RuleNames
);

public record HydratedFuzzyVariable(
    string Id,
    string Name,
    string SystemName,
    string VariableType,
    string Description,
    double UniverseMin,
    double UniverseMax,
    string ReferenceCode,
    List<HydratedFuzzyTerm> Terms
);

public record HydratedFuzzyTerm(
    string Id,
    string Label,
    string FunctionType,
    List<double> Parameters
);

public record HydratedFuzzyRule(
    string Id,
    string Name,
    string SystemName,
    string Description,
    string RuleText,
    List<HydratedRuleCondition> Conditions,
    List<string> Connectors,
    List<HydratedRuleConsequent> Consequents
);

public record HydratedRuleCondition(
    string VariableName,
    string Operator,
    string TermLabel
);

public record HydratedRuleConsequent(
    string VariableName,
    List<string> TermLabels,
    string AggregationMethod
);
