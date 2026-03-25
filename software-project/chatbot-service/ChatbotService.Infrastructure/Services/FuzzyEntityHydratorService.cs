using System.Text.Json;
using ChatbotService.Domain.Interfaces;
using ChatbotService.Domain.Models.Fuzzy;
using ChatbotService.Infrastructure.Extensions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace ChatbotService.Infrastructure.Services;

/// <summary>
/// Orquesta la hidratación de entidades fuzzy. Busca IDs en el servicio fuzzy 
/// y ensambla objetos fuertemente tipados y denormalizados con los nombres reales de las entidades.
/// </summary>
public class FuzzyEntityHydratorService : IFuzzyEntityHydratorService
{
    private readonly IFuzzyServiceClient _fuzzyClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<FuzzyEntityHydratorService> _logger;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    public FuzzyEntityHydratorService(
        IFuzzyServiceClient fuzzyClient,
        IMemoryCache cache,
        ILogger<FuzzyEntityHydratorService> logger)
    {
        _fuzzyClient = fuzzyClient;
        _cache = cache;
        _logger = logger;
    }

    public async Task<HydratedFuzzySystem?> GetHydratedSystemAsync(string systemId, CancellationToken ct)
    {
        return await _cache.GetOrCreateAsync($"sys_{systemId}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            var systemJson = await _fuzzyClient.GetEntityByIdAsync("fuzzy_system", systemId, ct);
            if (systemJson == null) return null;

            var sys = systemJson.Value;
            var name = sys.GetStringOrDefault("name", "Sin nombre");
            var description = sys.GetStringOrDefault("description", "");
            var status = sys.GetStringOrDefault("status", "desconocido");
            var defuzzMethod = sys.GetStringOrDefault("defuzzification_method", "Centroide");

            var inputVars = await GetVariableNamesAsync(sys, "input_variable_ids", ct);
            var outputVars = await GetVariableNamesAsync(sys, "output_variable_ids", ct);
            var ruleNames = await GetRuleNamesAsync(sys, "rule_ids", ct);

            return new HydratedFuzzySystem(
                systemId, name, description, status, defuzzMethod,
                inputVars, outputVars, ruleNames
            );
        });
    }

    public async Task<HydratedFuzzyVariable?> GetHydratedVariableAsync(string variableId, CancellationToken ct)
    {
        return await _cache.GetOrCreateAsync($"var_{variableId}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            var varJson = await _fuzzyClient.GetEntityByIdAsync("fuzzy_variable", variableId, ct);
            if (varJson == null) return null;

            var variable = varJson.Value;
            var name = variable.GetStringOrDefault("name", "Sin nombre");
            var type = variable.GetStringOrDefault("variable_type", "input");
            var description = variable.GetStringOrDefault("description", "");
            var uMin = variable.TryGetProperty("universe_min", out var minProp) ? minProp.GetDoubleOrDefault(0) : 0;
            var uMax = variable.TryGetProperty("universe_max", out var maxProp) ? maxProp.GetDoubleOrDefault(100) : 100;
            var refCode = variable.GetStringOrDefault("reference_code", "");
            var sysId = variable.GetStringOrDefault("system_id", "");

            var sysName = "Sistema Desconocido";
            if (!string.IsNullOrEmpty(sysId))
            {
                var sys = await GetHydratedSystemAsync(sysId, ct);
                if (sys != null) sysName = sys.Name;
            }
            else
            {
                var sysNameFromSearch = await FindSystemNameForVariableAsync(variableId, ct);
                if (!string.IsNullOrEmpty(sysNameFromSearch))
                {
                    sysName = sysNameFromSearch;
                }
            }

            var terms = new List<HydratedFuzzyTerm>();
            if (variable.TryGetProperty("terms", out var termsArray) && termsArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var termIdObj in termsArray.EnumerateArray())
                {
                    var termId = termIdObj.GetString();
                    if (string.IsNullOrEmpty(termId)) continue;

                    var term = await GetHydratedTermAsync(termId, ct);
                    if (term != null) terms.Add(term);
                }
            }

            return new HydratedFuzzyVariable(
                variableId, name, sysName, type, description,
                uMin, uMax, refCode, terms
            );
        });
    }

    private async Task<HydratedFuzzyTerm?> GetHydratedTermAsync(string termId, CancellationToken ct)
    {
        return await _cache.GetOrCreateAsync($"term_{termId}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            var termJson = await _fuzzyClient.GetEntityByIdAsync("fuzzy_term", termId, ct);
            if (termJson == null) return null;

            var term = termJson.Value;
            var label = term.GetStringOrDefault("label", "Sin etiqueta");
            var fnType = "desconocida";
            var parameters = new List<double>();

            if (term.TryGetProperty("membership_function", out var mf) && mf.ValueKind == JsonValueKind.Object)
            {
                fnType = mf.GetStringOrDefault("function_type", "desconocida");
                if (mf.TryGetProperty("parameters", out var pars) && pars.ValueKind == JsonValueKind.Array)
                {
                    foreach (var p in pars.EnumerateArray())
                    {
                        parameters.Add(p.GetDoubleOrDefault(0));
                    }
                }
            }

            return new HydratedFuzzyTerm(termId, label, fnType, parameters);
        });
    }

    public async Task<HydratedFuzzyRule?> GetHydratedRuleAsync(string ruleId, CancellationToken ct)
    {
        return await _cache.GetOrCreateAsync($"rule_{ruleId}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            var ruleJson = await _fuzzyClient.GetEntityByIdAsync("fuzzy_rule", ruleId, ct);
            if (ruleJson == null) return null;

            var rule = ruleJson.Value;
            var name = rule.GetStringOrDefault("name", "Sin nombre");
            var desc = rule.GetStringOrDefault("description", "");
            var ruleText = rule.GetStringOrDefault("rule_text", "");
            var sysId = rule.GetStringOrDefault("system_id", "");

            var sysName = "Sistema Desconocido";
            if (!string.IsNullOrEmpty(sysId))
            {
                var sys = await GetHydratedSystemAsync(sysId, ct);
                if (sys != null) sysName = sys.Name;
            }

            var hydratedConds = new List<HydratedRuleCondition>();
            if (rule.TryGetProperty("conditions", out var conds) && conds.ValueKind == JsonValueKind.Array)
            {
                foreach (var cond in conds.EnumerateArray())
                {
                    var varId = cond.GetStringOrDefault("variable_id", "");
                    var op = cond.GetStringOrDefault("operator", "IS");
                    var termId = cond.GetStringOrDefault("value", ""); // el ID del termino esta aqui en 'value' o 'term_id'? en el codigo actual es 'value'
                    
                    var varName = await ResolveVariableNameAsync(varId, ct);
                    var termName = await ResolveTermNameAsync(termId, ct);

                    hydratedConds.Add(new HydratedRuleCondition(varName, op, termName));
                }
            }

            var connectors = new List<string>();
            if (rule.TryGetProperty("connectors", out var conns) && conns.ValueKind == JsonValueKind.Array)
            {
                foreach (var conn in conns.EnumerateArray())
                    connectors.Add(conn.GetString() ?? "AND");
            }

            var hydratedCons = new List<HydratedRuleConsequent>();
            if (rule.TryGetProperty("consequents", out var cons) && cons.ValueKind == JsonValueKind.Array)
            {
                foreach (var c in cons.EnumerateArray())
                {
                    var varId = c.GetStringOrDefault("variable_id", "");
                    var agg = c.GetStringOrDefault("aggregation_method", "max");
                    var varName = await ResolveVariableNameAsync(varId, ct);
                    
                    var termLabels = new List<string>();
                    if (c.TryGetProperty("terms", out var termsArr) && termsArr.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var tIdObj in termsArr.EnumerateArray())
                        {
                            var tId = tIdObj.GetString();
                            if (!string.IsNullOrEmpty(tId))
                                termLabels.Add(await ResolveTermNameAsync(tId, ct));
                        }
                    }

                    hydratedCons.Add(new HydratedRuleConsequent(varName, termLabels, agg));
                }
            }

            return new HydratedFuzzyRule(
                ruleId, name, sysName, desc, ruleText,
                hydratedConds, connectors, hydratedCons
            );
        });
    }

    private async Task<List<string>> GetVariableNamesAsync(JsonElement sys, string propertyName, CancellationToken ct)
    {
        var names = new List<string>();
        if (sys.TryGetProperty(propertyName, out var vars) && vars.ValueKind == JsonValueKind.Array)
        {
            foreach (var vIdObj in vars.EnumerateArray())
            {
                var vId = vIdObj.GetString();
                if (!string.IsNullOrEmpty(vId))
                {
                    var name = await ResolveVariableNameAsync(vId, ct);
                    names.Add(name);
                }
            }
        }
        return names;
    }

    private async Task<List<string>> GetRuleNamesAsync(JsonElement sys, string propertyName, CancellationToken ct)
    {
        var names = new List<string>();
        if (sys.TryGetProperty(propertyName, out var rules) && rules.ValueKind == JsonValueKind.Array)
        {
            foreach (var rIdObj in rules.EnumerateArray())
            {
                var rId = rIdObj.GetString();
                if (!string.IsNullOrEmpty(rId))
                {
                    var rJson = await _fuzzyClient.GetEntityByIdAsync("fuzzy_rule", rId, ct);
                    if (rJson != null)
                        names.Add(rJson.Value.GetStringOrDefault("name", "Sin nombre"));
                }
            }
        }
        return names;
    }

    private async Task<string> ResolveVariableNameAsync(string varId, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(varId)) return "Variable_Desconocida";
        var vJson = await _cache.GetOrCreateAsync($"varJson_{varId}", async entry => 
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            return await _fuzzyClient.GetEntityByIdAsync("fuzzy_variable", varId, ct);
        });
        return vJson?.GetStringOrDefault("name", varId) ?? varId;
    }

    private async Task<string> ResolveTermNameAsync(string termId, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(termId)) return "Término_Desconocido";
        var tJson = await _cache.GetOrCreateAsync($"termJson_{termId}", async entry => 
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            return await _fuzzyClient.GetEntityByIdAsync("fuzzy_term", termId, ct);
        });
        return tJson?.GetStringOrDefault("label", termId) ?? termId;
    }

    private async Task<string?> FindSystemNameForVariableAsync(string variableId, CancellationToken ct)
    {
        try
        {
            var systemsJson = await _fuzzyClient.GetAllEntitiesByTypeAsync("fuzzy_system", ct);
            foreach (var sys in systemsJson)
            {
                var isInput = ContainsId(sys, "input_variable_ids", variableId);
                var isOutput = ContainsId(sys, "output_variable_ids", variableId);
                
                if (isInput || isOutput)
                {
                    var sysId = sys.GetStringOrDefault("id", "");
                    if (!string.IsNullOrEmpty(sysId))
                    {
                        var hydratedSys = await GetHydratedSystemAsync(sysId, ct);
                        return hydratedSys?.Name ?? sys.GetStringOrDefault("name", "Sin nombre");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error al buscar el sistema para la variable {VariableId}", variableId);
        }
        return null;
    }

    private bool ContainsId(JsonElement sys, string propertyName, string targetId)
    {
        if (sys.TryGetProperty(propertyName, out var arr) && arr.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in arr.EnumerateArray())
            {
                if (item.GetString() == targetId) return true;
            }
        }
        return false;
    }
}
