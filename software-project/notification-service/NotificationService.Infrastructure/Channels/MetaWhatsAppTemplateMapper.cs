using NotificationService.Domain.Entities;

namespace NotificationService.Infrastructure.Channels;

/// <summary>
/// Maps internal notification template keys to Meta WhatsApp Business template names
/// and builds the positional component parameters expected by the Cloud API.
/// 
/// Each Meta template has a fixed structure with named variables ({{1}}, {{2}}, …).
/// This mapper extracts the required values from <see cref="NotificationMessage.Data"/>
/// and formats them into the components array for the API payload.
/// </summary>
public static class MetaWhatsAppTemplateMapper
{
    // ─────────────── Alert type → human-readable label (Spanish) ─────────────z──
    private static readonly Dictionary<string, (string Label, string Emoji)> AlertTypeLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["extreme_heat"]    = ("Calor extremo", "🔥"),
        ["extreme_cold"]    = ("Frío extremo", "🥶"),
        ["high_humidity"]   = ("Humedad alta", "💧"),
        ["low_humidity"]    = ("Humedad baja", "🏜️"),
        ["heavy_rain"]      = ("Lluvia intensa", "🌧️"),
        ["thunderstorm"]    = ("Tormenta eléctrica", "⛈️"),
        ["high_cloudiness"] = ("Nubosidad alta", "☁️"),
        ["strong_wind"]     = ("Viento fuerte", "💨"),
        ["extreme_uv"]      = ("UV extremo", "☀️"),
        ["government"]      = ("Alerta oficial del gobierno", "🚨")
    };

    /// <summary>
    /// Maps a templateKey to the corresponding Meta WhatsApp template name.
    /// Returns null if the template key is unknown.
    /// </summary>
    public static string? GetTemplateName(string templateKey) => templateKey switch
    {
        "daily_summary" => "resumen_diario_invernadero",
        "weather_alert" => "alerta_meteorologica",
        _ => null
    };

    /// <summary>
    /// Returns the BCP-47 language code for the Meta template language.
    /// Must match exactly the language selected when the template was created in Meta.
    /// </summary>
    public static string GetLanguageCode(string templateKey) => templateKey switch
    {
        "daily_summary" => "es_CO",   // Spanish (COL)
        "weather_alert" => "en",   // English (US) — recreate in es_CO once approved
        _ => "es_CO"
    };

    /// <summary>
    /// Builds header component parameters for the given template.
    /// </summary>
    public static List<TemplateParameter> BuildHeaderParameters(string templateKey, NotificationMessage message)
    {
        return templateKey switch
        {
            "daily_summary" => [new TemplateParameter(GetData(message, "date", DateTime.UtcNow.AddHours(-5).ToString("dd/MM/yyyy")))],
            "weather_alert" => [new TemplateParameter(BuildAlertHeaderText(message))],
            _ => []
        };
    }

    /// <summary>
    /// Builds body component parameters for the given template.
    /// </summary>
    public static List<TemplateParameter> BuildBodyParameters(string templateKey, NotificationMessage message)
    {
        return templateKey switch
        {
            "daily_summary" => BuildDailySummaryBodyParams(message),
            "weather_alert" => BuildWeatherAlertBodyParams(message),
            _ => []
        };
    }

    // ─────────────── Daily Summary (27 body variables) ───────────────

    private static List<TemplateParameter> BuildDailySummaryBodyParams(NotificationMessage message)
    {
        var d = message.Data;

        return
        [
            // Sensors: Temp ambiente (avg, min, max) → {{1}}–{{3}}
            P(d, "temp_avg"),
            P(d, "temp_min", "--"),
            P(d, "temp_max", "--"),

            // Humedad (avg, min, max) → {{4}}–{{6}}
            P(d, "hum_avg"),
            P(d, "hum_min", "--"),
            P(d, "hum_max", "--"),

            // Luminosidad (avg, min, max) → {{7}}–{{9}}
            P(d, "lux_avg"),
            P(d, "lux_min", "--"),
            P(d, "lux_max", "--"),

            // pH (avg, min, max) → {{10}}–{{12}}
            P(d, "ph_avg"),
            P(d, "ph_min", "--"),
            P(d, "ph_max", "--"),

            // Temp tanque (avg, min, max) → {{13}}–{{15}}
            P(d, "tank_temp_avg"),
            P(d, "tank_temp_min", "--"),
            P(d, "tank_temp_max", "--"),

            // Nivel de agua (avg, min, max) → {{16}}–{{18}}
            P(d, "water_level_avg"),
            P(d, "water_level_min", "--"),
            P(d, "water_level_max", "--"),

            // Actuadores (minutos) → {{19}}–{{25}}
            P(d, "act_ventiladores", "0"),
            P(d, "act_termoventilador", "0"),
            P(d, "act_luz_amplio_espectro", "0"),
            P(d, "act_piedra_difusora", "0"),
            P(d, "act_bomba_agua", "0"),
            P(d, "act_calefactor_agua", "0"),
            P(d, "act_humidificador", "0"),

            // Fuzzy → {{26}}–{{27}}
            P(d, "fuzzy_system_name", "Sin sistema activo"),
            P(d, "fuzzy_eval_count", "0")
        ];
    }

    // ─────────────── Weather Alert (4 body variables) ───────────────

    private static List<TemplateParameter> BuildWeatherAlertBodyParams(NotificationMessage message)
    {
        var severity = GetData(message, "severity", "warning");
        var humanSeverity = severity.Equals("critical", StringComparison.OrdinalIgnoreCase)
            ? "Crítica" : "Advertencia";

        var isGrouped = GetData(message, "isGrouped", "false") == "true";

        // Strip embedded recommendations (💡 lines) from the body — they go in {{3}} separately
        var cleanBody = StripEmbeddedRecommendations(message.Body);
        var detail = Truncate(cleanBody, 400);

        var recommendation = isGrouped
            ? "Revisa cada alerta en la app para ver las recomendaciones individuales."
            : Truncate(GetData(message, "recommendation", "Toma las precauciones necesarias."), 250);

        var forecastDate = GetData(message, "forecastDate",
            DateTime.UtcNow.AddHours(-5).ToString("dd/MM/yyyy"));

        return
        [
            new TemplateParameter(humanSeverity),   // {{1}} severity
            new TemplateParameter(detail),          // {{2}} detail
            new TemplateParameter(recommendation),  // {{3}} recommendation
            new TemplateParameter(forecastDate)     // {{4}} forecast date
        ];
    }

    /// <summary>
    /// Removes embedded recommendation lines (starting with 💡 or "  💡") from body text.
    /// The weather-service embeds recommendations in the body for Twilio (free-text),
    /// but for Meta templates the recommendation is a separate variable ({{3}}).
    /// </summary>
    private static string StripEmbeddedRecommendations(string body)
    {
        if (string.IsNullOrEmpty(body)) return body;

        var lines = body.Split('\n');
        var filtered = lines
            .Where(line => !line.TrimStart().StartsWith("💡"))
            .ToList();

        // Remove trailing empty lines left after stripping
        while (filtered.Count > 0 && string.IsNullOrWhiteSpace(filtered[^1]))
            filtered.RemoveAt(filtered.Count - 1);

        return string.Join('\n', filtered).Trim();
    }

    private static string Truncate(string value, int maxLength)
        => value.Length <= maxLength ? value : value[..maxLength].TrimEnd() + "…";

    // ─────────────── Header helpers ───────────────

    private static string BuildAlertHeaderText(NotificationMessage message)
    {
        var isGrouped = GetData(message, "isGrouped", "false") == "true";
        if (isGrouped)
        {
            var count = GetData(message, "alertCount", "varias");
            return $"{count} alertas detectadas";
        }

        // Single alert — use the alert type label
        var alertType = GetData(message, "alertType", "");
        if (AlertTypeLabels.TryGetValue(alertType, out var info))
            return info.Label;

        // Fallback: use the title from the message
        return message.Title.Length <= 55 ? message.Title : message.Title[..55];
    }

    // ─────────────── Utility ───────────────

    private static string GetData(NotificationMessage message, string key, string fallback = "Sin datos")
        => message.Data.TryGetValue(key, out var val) && !string.IsNullOrEmpty(val)
            ? val : fallback;

    private static TemplateParameter P(Dictionary<string, string> data, string key, string fallback = "Sin datos")
        => new(data.TryGetValue(key, out var val) && !string.IsNullOrEmpty(val) ? val : fallback);

    /// <summary>
    /// Gets the emoji for a given alert type code, or a default warning emoji.
    /// Useful for building grouped alert summaries.
    /// </summary>
    public static string GetAlertEmoji(string alertType)
        => AlertTypeLabels.TryGetValue(alertType, out var info) ? info.Emoji : "⚠️";

    /// <summary>
    /// Gets the human-readable label for a given alert type code.
    /// </summary>
    public static string GetAlertLabel(string alertType)
        => AlertTypeLabels.TryGetValue(alertType, out var info) ? info.Label : alertType;
}

/// <summary>
/// A single template parameter value for the Meta Cloud API.
/// Sanitizes the text on construction: removes newlines/tabs and collapses
/// consecutive spaces, as Meta rejects parameters with those characters.
/// </summary>
public record TemplateParameter
{
    public string Text { get; init; }

    public TemplateParameter(string text)
    {
        Text = Sanitize(text);
    }

    private static string Sanitize(string value)
    {
        if (string.IsNullOrEmpty(value)) return value;

        // Replace newlines and tabs with a single space
        var result = value
            .Replace("\r\n", " ")
            .Replace('\n', ' ')
            .Replace('\r', ' ')
            .Replace('\t', ' ');

        // Collapse runs of 5+ consecutive spaces down to 4 (Meta's limit)
        while (result.Contains("     "))
            result = result.Replace("     ", "    ");

        return result.Trim();
    }
}
