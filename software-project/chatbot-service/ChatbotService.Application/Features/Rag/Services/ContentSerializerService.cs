using System.Text.Json;

namespace ChatbotService.Application.Features.Rag.Services;

/// <summary>
/// Servicio que convierte entidades fuzzy (JSON de la API del fuzzy-service) a texto descriptivo
/// legible para su posterior vectorización. El texto generado se almacena en <c>knowledge_chunks.content</c>.
/// Trabaja con <see cref="JsonElement"/> obtenido vía HTTP clients, sin dependencia directa a MongoDB.
/// </summary>
public class ContentSerializerService
{
    /// <summary>
    /// Serializa cualquier entidad fuzzy según su tipo.
    /// </summary>
    /// <param name="sourceType">Tipo de entidad: fuzzy_rule, fuzzy_system, fuzzy_variable, fuzzy_term.</param>
    /// <param name="entity">El JsonElement de la entidad obtenido del fuzzy-service.</param>
    /// <returns>Texto descriptivo legible para vectorización.</returns>
    public string Serialize(string sourceType, JsonElement entity) => sourceType switch
    {
        "fuzzy_rule" => SerializeFuzzyRule(entity),
        "fuzzy_system" => SerializeFuzzySystem(entity),
        "fuzzy_variable" => SerializeFuzzyVariable(entity),
        "fuzzy_term" => SerializeFuzzyTerm(entity),
        _ => entity.ToString()
    };

    /// <summary>
    /// Serializa una regla fuzzy (FuzzyRuleDto) a texto descriptivo para vectorización.
    /// </summary>
    public string SerializeFuzzyRule(JsonElement rule)
    {
        var name = rule.GetStringOrDefault("name", "Sin nombre");
        var systemId = rule.GetStringOrDefault("system_id", "Desconocido");
        var description = rule.GetStringOrDefault("description", "");
        var ruleText = rule.GetStringOrDefault("rule_text", "");

        var conditions = new List<string>();
        if (rule.TryGetProperty("conditions", out var conds) && conds.ValueKind == JsonValueKind.Array)
        {
            foreach (var cond in conds.EnumerateArray())
            {
                var variableId = cond.GetStringOrDefault("variable_id", "");
                var op = cond.GetStringOrDefault("operator", "IS");
                var value = cond.GetStringOrDefault("value", "");
                conditions.Add($"{variableId} {op} {value}");
            }
        }

        var connectors = new List<string>();
        if (rule.TryGetProperty("connectors", out var conns) && conns.ValueKind == JsonValueKind.Array)
        {
            foreach (var conn in conns.EnumerateArray())
            {
                connectors.Add(conn.GetString() ?? "AND");
            }
        }

        var consequents = new List<string>();
        if (rule.TryGetProperty("consequents", out var cons) && cons.ValueKind == JsonValueKind.Array)
        {
            foreach (var c in cons.EnumerateArray())
            {
                var variableId = c.GetStringOrDefault("variable_id", "");
                var terms = c.TryGetProperty("terms", out var t) && t.ValueKind == JsonValueKind.Array
                    ? string.Join(", ", t.EnumerateArray().Select(x => x.GetString() ?? ""))
                    : "";
                var agg = c.GetStringOrDefault("aggregation_method", "max");
                consequents.Add($"{variableId} = {{{terms}}} (agregación: {agg})");
            }
        }

        var parts = new List<string>
        {
            $"Regla: \"{name}\" del sistema \"{systemId}\"."
        };

        if (!string.IsNullOrWhiteSpace(ruleText))
            parts.Add($"Texto de regla: {ruleText}");
        if (conditions.Count > 0)
        {
            var condText = conditions.Count == 1
                ? conditions[0]
                : string.Join($" {string.Join(" ", connectors).PadRight(conditions.Count - 1)} ", conditions);
            parts.Add($"Condiciones: SI {string.Join(" AND ", conditions)}.");
        }
        if (consequents.Count > 0)
            parts.Add($"Consecuentes: ENTONCES {string.Join(", ", consequents)}.");
        if (!string.IsNullOrWhiteSpace(description))
            parts.Add($"Descripción: {description}");

        return string.Join("\n", parts);
    }

    /// <summary>
    /// Serializa un sistema fuzzy (FuzzySystemDto) a texto descriptivo para vectorización.
    /// </summary>
    public string SerializeFuzzySystem(JsonElement system)
    {
        var name = system.GetStringOrDefault("name", "Sin nombre");
        var description = system.GetStringOrDefault("description", "");
        var status = system.GetStringOrDefault("status", "desconocido");
        var defuzzMethod = system.GetStringOrDefault("defuzzification_method", "");

        var inputCount = system.TryGetProperty("input_variable_ids", out var inputs) && inputs.ValueKind == JsonValueKind.Array
            ? inputs.GetArrayLength() : 0;
        var outputCount = system.TryGetProperty("output_variable_ids", out var outputs) && outputs.ValueKind == JsonValueKind.Array
            ? outputs.GetArrayLength() : 0;
        var ruleCount = system.TryGetProperty("rule_ids", out var rules) && rules.ValueKind == JsonValueKind.Array
            ? rules.GetArrayLength() : 0;

        var parts = new List<string>
        {
            $"Sistema Fuzzy: \"{name}\" (estado: {status})."
        };

        if (!string.IsNullOrWhiteSpace(description))
            parts.Add($"Descripción: {description}");
        if (!string.IsNullOrWhiteSpace(defuzzMethod))
            parts.Add($"Método de defuzzificación: {defuzzMethod}.");
        parts.Add($"Variables de entrada: {inputCount}, variables de salida: {outputCount}.");
        if (ruleCount > 0)
            parts.Add($"Cantidad de reglas: {ruleCount}.");

        return string.Join("\n", parts);
    }

    /// <summary>
    /// Serializa una variable fuzzy (FuzzyVariableDto) a texto descriptivo.
    /// </summary>
    public string SerializeFuzzyVariable(JsonElement variable)
    {
        var name = variable.GetStringOrDefault("name", "Sin nombre");
        var type = variable.GetStringOrDefault("variable_type", "input");
        var description = variable.GetStringOrDefault("description", "");
        var universeMin = variable.TryGetProperty("universe_min", out var uMin) ? uMin.GetDoubleOrDefault(0) : 0;
        var universeMax = variable.TryGetProperty("universe_max", out var uMax) ? uMax.GetDoubleOrDefault(100) : 100;
        var referenceCode = variable.GetStringOrDefault("reference_code", "");

        var termCount = variable.TryGetProperty("terms", out var terms) && terms.ValueKind == JsonValueKind.Array
            ? terms.GetArrayLength() : 0;

        var parts = new List<string>
        {
            $"Variable Fuzzy: \"{name}\" ({type}).",
            $"Rango: {universeMin}-{universeMax}."
        };

        if (!string.IsNullOrWhiteSpace(description))
            parts.Add($"Descripción: {description}");
        if (!string.IsNullOrWhiteSpace(referenceCode))
            parts.Add($"Código de referencia: {referenceCode}.");
        if (termCount > 0)
            parts.Add($"Cantidad de términos lingüísticos: {termCount}.");

        return string.Join("\n", parts);
    }

    /// <summary>
    /// Serializa un término fuzzy (FuzzyTermDto) a texto descriptivo.
    /// </summary>
    public string SerializeFuzzyTerm(JsonElement term)
    {
        var label = term.GetStringOrDefault("label", "Sin etiqueta");
        var variableId = term.GetStringOrDefault("variable_id", "N/A");

        var functionType = "";
        var parameters = "";
        if (term.TryGetProperty("membership_function", out var mf) && mf.ValueKind == JsonValueKind.Object)
        {
            functionType = mf.GetStringOrDefault("function_type", "desconocida");
            if (mf.TryGetProperty("parameters", out var pars) && pars.ValueKind == JsonValueKind.Array)
            {
                parameters = string.Join(", ", pars.EnumerateArray().Select(p => p.GetDoubleOrDefault(0).ToString("F2")));
            }
        }

        var parts = new List<string>
        {
            $"Término Fuzzy: \"{label}\" (variable: {variableId})."
        };

        if (!string.IsNullOrWhiteSpace(functionType))
            parts.Add($"Función de membresía: {functionType}.");
        if (!string.IsNullOrWhiteSpace(parameters))
            parts.Add($"Parámetros: [{parameters}].");

        return string.Join("\n", parts);
    }
}

/// <summary>
/// Métodos de extensión para simplificar la extracción de valores de <see cref="JsonElement"/>.
/// </summary>
internal static class JsonElementExtensions
{
    public static string GetStringOrDefault(this JsonElement element, string propertyName, string defaultValue)
    {
        return element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String
            ? prop.GetString() ?? defaultValue
            : defaultValue;
    }

    public static double GetDoubleOrDefault(this JsonElement element, double defaultValue)
    {
        return element.ValueKind == JsonValueKind.Number
            ? element.GetDouble()
            : defaultValue;
    }
}
