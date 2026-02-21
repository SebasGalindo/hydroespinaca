using NotificationService.Domain.Entities;
using NotificationService.Domain.Interfaces;

namespace NotificationService.Infrastructure.DailySummary;

/// <summary>
/// Formats DailySummaryData into HTML (for email) and plain text (for push/WhatsApp).
/// </summary>
public class DailySummaryContentFormatter
{
    /// <summary>
    /// Renders the full HTML body content for the daily summary email.
    /// This HTML gets wrapped in the base.liquid layout by the template renderer.
    /// </summary>
    public string FormatHtmlBody(DailySummaryData data)
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine($"<h1>📊 Resumen Diario — {data.Date:dd/MM/yyyy}</h1>");

        // Sensors section
        if (data.IncludeSensorAverages && data.SensorSummaries.Count > 0)
        {
            sb.AppendLine("<h2>🌡️ Sensores (promedios de ayer)</h2>");
            sb.AppendLine("<table style=\"width:100%;border-collapse:collapse;margin:12px 0;\">");
            sb.AppendLine("<tr style=\"background:#f0fdf4;\">");
            sb.AppendLine("<th style=\"text-align:left;padding:8px;border-bottom:1px solid #e5e7eb;\">Variable</th>");
            sb.AppendLine("<th style=\"text-align:right;padding:8px;border-bottom:1px solid #e5e7eb;\">Promedio</th>");
            sb.AppendLine("<th style=\"text-align:right;padding:8px;border-bottom:1px solid #e5e7eb;\">Mín</th>");
            sb.AppendLine("<th style=\"text-align:right;padding:8px;border-bottom:1px solid #e5e7eb;\">Máx</th>");
            sb.AppendLine("</tr>");

            foreach (var sensor in data.SensorSummaries)
            {
                var name = !string.IsNullOrEmpty(sensor.VariableName) ? sensor.VariableName : sensor.VariableCode;
                sb.AppendLine($"<tr>");
                sb.AppendLine($"<td style=\"padding:6px 8px;border-bottom:1px solid #f3f4f6;\">{name}</td>");
                sb.AppendLine($"<td style=\"text-align:right;padding:6px 8px;border-bottom:1px solid #f3f4f6;font-weight:600;\">{sensor.Avg:F1}</td>");
                sb.AppendLine($"<td style=\"text-align:right;padding:6px 8px;border-bottom:1px solid #f3f4f6;color:#6b7280;\">{sensor.Min:F1}</td>");
                sb.AppendLine($"<td style=\"text-align:right;padding:6px 8px;border-bottom:1px solid #f3f4f6;color:#6b7280;\">{sensor.Max:F1}</td>");
                sb.AppendLine("</tr>");
            }
            sb.AppendLine("</table>");
        }

        // Actuators section
        if (data.IncludeActuatorRuntime && data.ActuatorSummaries.Count > 0)
        {
            sb.AppendLine("<h2>⚡ Actuadores (tiempo activo ayer)</h2>");
            sb.AppendLine("<ul style=\"list-style:none;padding-left:0;\">");
            foreach (var act in data.ActuatorSummaries)
            {
                sb.AppendLine($"<li style=\"padding:4px 0;\">• <strong>{act.ActuatorCode}</strong>: " +
                    $"{act.TotalDurationMinutes:F0} min ({act.Percentage:F1}% del día) — " +
                    $"{act.ActivationCount} activaciones</li>");
            }
            sb.AppendLine("</ul>");
        }

        // Fuzzy section
        if (data.IncludeFuzzyRules && data.FuzzyEvaluation != null)
        {
            sb.AppendLine("<h2>🧠 Sistema Fuzzy</h2>");
            sb.AppendLine($"<p>Evaluaciones realizadas: <strong>{data.FuzzyEvaluation.EvaluationCount}</strong></p>");
            if (!string.IsNullOrEmpty(data.FuzzyEvaluation.SystemName))
                sb.AppendLine($"<p>Sistema activo: {data.FuzzyEvaluation.SystemName}</p>");

            if (data.FuzzyEvaluation.TopRules.Count > 0)
            {
                sb.AppendLine("<p>Top reglas activadas:</p>");
                sb.AppendLine("<ul>");
                foreach (var rule in data.FuzzyEvaluation.TopRules)
                {
                    sb.AppendLine($"<li>{rule.RuleId}: {rule.ActivationCount}× " +
                        $"(fuerza promedio: {rule.AvgFiringStrength:F2})</li>");
                }
                sb.AppendLine("</ul>");
            }
        }

        // Weather section
        if (data.IncludeWeatherForecast && data.WeatherForecast != null)
        {
            var weather = data.WeatherForecast;
            sb.AppendLine("<h2>🌦️ Pronóstico Mañana</h2>");
            sb.AppendLine($"<p>Temperatura: <strong>{weather.TempMin:F0}°C – {weather.TempMax:F0}°C</strong></p>");
            sb.AppendLine($"<p>Probabilidad de lluvia: <strong>{weather.Pop:F0}%</strong></p>");
            sb.AppendLine($"<p>Condición: {weather.Description}</p>");
            if (!string.IsNullOrEmpty(weather.Summary))
                sb.AppendLine($"<p style=\"color:#6b7280;font-style:italic;\">{weather.Summary}</p>");
        }

        // Empty state
        if (sb.Length < 80)
        {
            sb.AppendLine("<p>No hay datos disponibles para el resumen de hoy.</p>");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Formats a short plain text summary for push notifications and WhatsApp.
    /// Limited to 2-3 key lines.
    /// </summary>
    public string FormatPlainText(DailySummaryData data)
    {
        var lines = new List<string>();

        // Sensor headline
        if (data.IncludeSensorAverages && data.SensorSummaries.Count > 0)
        {
            var temp = data.SensorSummaries.FirstOrDefault(
                s => s.VariableCode.Contains("TEMP", StringComparison.OrdinalIgnoreCase));
            var humidity = data.SensorSummaries.FirstOrDefault(
                s => s.VariableCode.Contains("HUM", StringComparison.OrdinalIgnoreCase));

            if (temp != null)
                lines.Add($"🌡️ Temp: {temp.Avg:F1}°C ({temp.Min:F0}–{temp.Max:F0})");
            if (humidity != null)
                lines.Add($"💧 Humedad: {humidity.Avg:F0}%");
        }

        // Weather headline
        if (data.IncludeWeatherForecast && data.WeatherForecast != null)
        {
            var w = data.WeatherForecast;
            lines.Add($"🌦️ Mañana: {w.TempMin:F0}–{w.TempMax:F0}°C, lluvia {w.Pop:F0}%");
        }

        // Fuzzy headline
        if (data.IncludeFuzzyRules && data.FuzzyEvaluation != null)
            lines.Add($"🧠 {data.FuzzyEvaluation.EvaluationCount} eval. fuzzy");

        // Actuator headline
        if (data.IncludeActuatorRuntime && data.ActuatorSummaries.Count > 0)
        {
            var total = data.ActuatorSummaries.Sum(a => a.TotalDurationMinutes);
            lines.Add($"⚡ Actuadores: {total:F0} min activos");
        }

        return lines.Count > 0
            ? string.Join("\n", lines)
            : "Sin datos para el resumen diario.";
    }
}
