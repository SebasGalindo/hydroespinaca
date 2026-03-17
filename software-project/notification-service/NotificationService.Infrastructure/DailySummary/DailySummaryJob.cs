using Microsoft.Extensions.Logging;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Interfaces;
using Quartz;

namespace NotificationService.Infrastructure.DailySummary;

/// <summary>
/// Quartz job that executes the daily summary for a specific user.
/// The UserId is passed via the JobDataMap when the trigger fires.
/// </summary>
[DisallowConcurrentExecution]
public class DailySummaryJob : IJob
{
    public static readonly JobKey BaseJobKey = new("DailySummaryJob", "DailySummary");
    public const string UserIdKey = "UserId";

    private readonly IDailySummaryDataAggregator _aggregator;
    private readonly INotificationDispatcher _dispatcher;
    private readonly INotificationPreferenceRepository _preferenceRepo;
    private readonly DailySummaryContentFormatter _formatter;
    private readonly ITemplateRenderer _templateRenderer;
    private readonly ISanitizer _sanitizer;
    private readonly ILogger<DailySummaryJob> _logger;

    /// <summary>
    /// Constructor with dependencies injected. 
    /// </summary>
    /// <param name="aggregator">The aggregator that fetches data from all services</param>
    /// <param name="dispatcher">The dispatcher that sends notifications</param>
    /// <param name="preferenceRepo">The repository that stores user notification preferences</param>
    /// <param name="formatter">The formatter that formats the daily summary content</param>
    /// <param name="templateRenderer">The renderer that renders HTML templates</param>
    /// <param name="sanitizer">The sanitizer that removes potentially dangerous HTML content</param>
    /// <param name="logger">The logger for logging job execution details</param>
    public DailySummaryJob(
        IDailySummaryDataAggregator aggregator,
        INotificationDispatcher dispatcher,
        INotificationPreferenceRepository preferenceRepo,
        DailySummaryContentFormatter formatter,
        ITemplateRenderer templateRenderer,
        ISanitizer sanitizer,
        ILogger<DailySummaryJob> logger)
    {
        _aggregator = aggregator;
        _dispatcher = dispatcher;
        _preferenceRepo = preferenceRepo;
        _formatter = formatter;
        _templateRenderer = templateRenderer;
        _sanitizer = sanitizer;
        _logger = logger;
    }

    /// <summary>
    /// Executes the daily summary job by performing the following steps:
    /// 1. Retrieves the user ID from the job context.
    /// 2. Fetches the user's notification preferences to check if the daily summary is enabled.
    /// 3. If enabled, aggregates data from all relevant services for the past 24 hours.
    /// 4. Formats the aggregated data into HTML and plain text.
    /// 5. Renders the HTML email using a template and sanitizes it.
    /// 6. Dispatches the notification to the user via the configured channels (email, whatsapp).
    /// 7. Logs the execution details and any errors that occur during the process.
    /// </summary>
    /// <param name="context">The Quartz job execution context containing job data and cancellation token</param>
    /// <returns></returns>
    public async Task Execute(IJobExecutionContext context)
    {
        // 0. Get user ID from JobDataMap
        var userId = context.MergedJobDataMap.GetString(UserIdKey);
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("DailySummaryJob fired without UserId in JobDataMap");
            return;
        }

        _logger.LogInformation("Executing daily summary for user {UserId}", userId);

        try
        {
            // 1. Get the latest preferences (in case they changed since scheduling)
            var preference = await _preferenceRepo.GetByUserIdAsync(userId);
            if (preference == null || preference.DailySummary == null || !preference.DailySummary.Enabled)
            {
                _logger.LogInformation("Daily summary disabled for user {UserId} — skipping", userId);
                return;
            }

            // 2. Aggregate data from all enabled services
            var data = await _aggregator.AggregateAsync(userId, preference, context.CancellationToken);

            // 3. Format content
            var htmlBody = _formatter.FormatHtmlBody(data);
            var plainText = _formatter.FormatPlainText(data);

            // 4. Render HTML email with base layout
            var sanitizedHtml = _sanitizer.Sanitize(htmlBody);
            var fullEmailHtml = await _templateRenderer.RenderAsync(
                "daily_summary", sanitizedHtml,
                new { subject = $"📊 Resumen Diario — {data.Date:dd/MM/yyyy}" },
                context.CancellationToken);

            // 5. Get channels from daily_summary config (default: email only)
            var channels = preference.DailySummary.Channels?.Count > 0
                ? preference.DailySummary.Channels
                : new List<string> { NotificationChannels.Email };

            // 6. Dispatch to all configured channels
            var title = $"📊 Resumen Diario — {data.Date:dd/MM/yyyy}";
            var additionalData = new Dictionary<string, string>
            {
                ["template_key"] = "daily_summary",
                ["date"] = data.Date.ToString("dd/MM/yyyy"),
                ["html_body"] = fullEmailHtml
            };

            // Inject individual values for Meta WhatsApp template variables
            InjectDailySummaryDataValues(additionalData, data);

            await _dispatcher.DispatchAsync(
                userId,
                "daily_summary",
                title,
                plainText,
                additionalData,
                channels,
                context.CancellationToken);

            _logger.LogInformation(
                "Daily summary sent to user {UserId} via channels: {Channels}",
                userId, string.Join(", ", channels));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute daily summary for user {UserId}", userId);
            // Don't rethrow — Quartz will retry automatically which could cause duplicate sends
        }
    }

    /// <summary>
    /// Populates the data dictionary with individual sensor, actuator and fuzzy values
    /// so they can be used as positional variables in Meta WhatsApp templates.
    /// Twilio ignores these keys (it uses the pre-formatted plain text body).
    /// </summary>
    private static void InjectDailySummaryDataValues(Dictionary<string, string> data, DailySummaryData summary)
    {
        // ── Sensors ──
        // Helper: find a sensor by variable code (case-insensitive partial match)
        SensorVariableSummary? FindSensor(string code) =>
            summary.SensorSummaries.FirstOrDefault(
                s => s.VariableCode.Contains(code, StringComparison.OrdinalIgnoreCase));

        void AddSensor(string prefix, string code, string format = "F1")
        {
            var s = FindSensor(code);
            data[$"{prefix}_avg"] = s != null ? s.Avg.ToString(format) : "";
            data[$"{prefix}_min"] = s != null ? s.Min.ToString(format) : "";
            data[$"{prefix}_max"] = s != null ? s.Max.ToString(format) : "";
        }

        AddSensor("temp", "TEMP");
        AddSensor("hum", "HUM", "F0");
        AddSensor("lux", "LUX", "F0");
        AddSensor("ph", "PH", "F1");
        AddSensor("tank_temp", "TANK_TEMP", "F1");
        AddSensor("water_level", "WATER_LEVEL", "F1");

        // ── Actuators ──
        // Map actuator codes to data keys
        var actuatorMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Ventiladores"] = "act_ventiladores",
            ["termoventilador"] = "act_termoventilador",
            ["luz-amplio-espectro"] = "act_luz_amplio_espectro",
            ["piedra-difusora"] = "act_piedra_difusora",
            ["bomba-agua"] = "act_bomba_agua",
            ["calefactor-agua"] = "act_calefactor_agua",
            ["humidificador-ultrasonico"] = "act_humidificador"
        };

        // Initialize all actuators to "0"
        foreach (var kvp in actuatorMap)
            data[kvp.Value] = "0";

        // Fill with actual values
        foreach (var act in summary.ActuatorSummaries)
        {
            if (actuatorMap.TryGetValue(act.ActuatorCode, out var key))
                data[key] = act.TotalDurationMinutes.ToString("F0");
        }

        // ── Fuzzy ──
        data["fuzzy_system_name"] = summary.FuzzyEvaluation?.SystemName ?? "";
        data["fuzzy_eval_count"] = (summary.FuzzyEvaluation?.EvaluationCount ?? 0).ToString();
    }
}
