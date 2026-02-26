using System.Text.Json;
using ChatbotService.Domain.Models.Fuzzy;

namespace ChatbotService.Application.Features.Rag.Services;

/// <summary>
/// Servicio que convierte entidades fuzzy (modelos hidratados) a texto descriptivo
/// legible para su posterior vectorización en lenguaje natural. El texto generado se almacena en <c>knowledge_chunks.content</c>.
/// </summary>
public class ContentSerializerService
{
    public string SerializeSystem(HydratedFuzzySystem system)
    {
        var parts = new List<string>
        {
            $"El sistema de lógica difusa llamado '{system.Name}' actualmente se encuentra en estado '{system.Status}'.",
            $"Descripción: {system.Description}",
            $"Utiliza el método de defuzzificación '{system.DefuzzificationMethod}'."
        };

        if (system.InputVariables.Any())
            parts.Add($"Contiene {system.InputVariables.Count} variables de entrada: {string.Join(", ", system.InputVariables)}.");
        else
            parts.Add("No tiene variables de entrada definidas.");

        if (system.OutputVariables.Any())
            parts.Add($"Contiene {system.OutputVariables.Count} variables de salida: {string.Join(", ", system.OutputVariables)}.");
        else
            parts.Add("No tiene variables de salida definidas.");

        if (system.RuleNames.Any())
            parts.Add($"Tiene {system.RuleNames.Count} reglas asociadas, incluyendo: {string.Join(", ", system.RuleNames)}.");
        else
            parts.Add("No tiene reglas asociadas.");

        return string.Join(" ", parts);
    }

    public string SerializeVariable(HydratedFuzzyVariable variable)
    {
        var parts = new List<string>
        {
            $"La variable de {(variable.VariableType == "input" ? "entrada" : "salida")} '{variable.Name}' " +
            $"con código de referencia '{variable.ReferenceCode}' se encuentra vinculada al sistema '{variable.SystemName}'.",
            $"Mide valores en un rango de {variable.UniverseMin} a {variable.UniverseMax}.",
            $"Descripción: {variable.Description}"
        };

        if (variable.Terms.Any())
        {
            parts.Add($"Se compone de {variable.Terms.Count} términos lingüísticos, los cuales son:");
            for (int i = 0; i < variable.Terms.Count; i++)
            {
                var t = variable.Terms[i];
                var termDesc = $"{i + 1}) '{t.Label}': corresponde a una función de membresía de tipo '{t.FunctionType}' " +
                               $"con parámetros [{string.Join(", ", t.Parameters)}].";
                parts.Add(termDesc);
            }
        }
        else
        {
            parts.Add("Actualmente no tiene términos lingüísticos definidos.");
        }

        return string.Join(" ", parts);
    }

    public string SerializeRule(HydratedFuzzyRule rule)
    {
        var parts = new List<string>
        {
            $"La regla con nombre '{rule.Name}' está asociada al sistema difuso '{rule.SystemName}'.",
            $"Descripción de la regla: {rule.Description}",
            "Esta regla infiere lo siguiente:"
        };

        var conditionTexts = new List<string>();
        foreach (var cond in rule.Conditions)
        {
            conditionTexts.Add($"la variable '{cond.VariableName}' {cond.Operator} '{cond.TermLabel}'");
        }
        
        var conditionsStr = conditionTexts.Count > 0 
            ? "SI " + string.Join($" {rule.Connectors.FirstOrDefault() ?? "AND"} ", conditionTexts) 
            : "";

        var consequentTexts = new List<string>();
        foreach (var cons in rule.Consequents)
        {
            var termsStr = string.Join(" o ", cons.TermLabels);
            consequentTexts.Add($"la variable '{cons.VariableName}' ES '{termsStr}' (con agregación {cons.AggregationMethod})");
        }

        var consequentsStr = consequentTexts.Count > 0
            ? "ENTONCES " + string.Join(" Y ", consequentTexts)
            : "";

        if (!string.IsNullOrEmpty(conditionsStr) && !string.IsNullOrEmpty(consequentsStr))
            parts.Add($"{conditionsStr} {consequentsStr}.");
        else if (!string.IsNullOrEmpty(rule.RuleText))
            parts.Add($"Texto de regla: {rule.RuleText}");

        return string.Join(" ", parts);
    }
}
