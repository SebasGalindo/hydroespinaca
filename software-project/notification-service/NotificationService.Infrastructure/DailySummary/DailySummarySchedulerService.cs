using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Interfaces;
using Quartz;

namespace NotificationService.Infrastructure.DailySummary;

/// <summary>
/// HostedService that loads user daily-summary preferences at startup and programs
/// Quartz CronTriggers for each. Exposes Reschedule/Remove methods so the PUT
/// preferences endpoint can reprogram without restarting the service.
///
/// Colombia is UTC-5 — cron expressions use America/Bogota timezone.
/// </summary>
public class DailySummarySchedulerService : BackgroundService, IDailySummaryScheduler
{
    private readonly ISchedulerFactory _schedulerFactory;
    private readonly INotificationPreferenceRepository _preferenceRepo;
    private readonly ILogger<DailySummarySchedulerService> _logger;

    // Timezone for scheduling daily summaries at the correct local time (Colombia)
    private static readonly TimeZoneInfo ColombiaTz =
        TimeZoneInfo.FindSystemTimeZoneById("America/Bogota");

    // Constructor with dependencies injected. The scheduler factory is used to get the Quartz scheduler instance, and the preference repository is used to load user preferences at startup. The logger is used for logging important information and errors during the scheduling process.
    public DailySummarySchedulerService(
        ISchedulerFactory schedulerFactory,
        INotificationPreferenceRepository preferenceRepo,
        ILogger<DailySummarySchedulerService> logger)
    {
        _schedulerFactory = schedulerFactory;
        _preferenceRepo = preferenceRepo;
        _logger = logger;
    }

    /// <summary>
    /// On service start, loads all user preferences that have daily summaries enabled and schedules a Quartz job 
    /// for each user at their specified time. The job will execute the DailySummaryJob which performs the 
    /// actual data aggregation and notification sending. If any errors occur during loading or scheduling, they are logged.
    /// The method also includes a small delay at the beginning to allow the Quartz scheduler to fully 
    /// initialize before attempting to schedule jobs.
    /// Each user's daily summary job is scheduled using a Cron expression that fires at the specified hour 
    /// and minute in the America/Bogota timezone.
    /// The method ensures that only users with daily summaries enabled are scheduled, and it logs the total 
    /// count of scheduled jobs at the end of the initialization process.
    /// If an exception occurs during this process, it is caught and logged as an error, 
    /// but it does not prevent the service from starting.
    /// </summary>
    /// <param name="stoppingToken">The cancellation token that is signaled when the service is stopping</param>
    /// <returns></returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait a bit for Quartz scheduler to fully start
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        _logger.LogInformation("DailySummarySchedulerService starting — loading subscriber preferences");

        try
        {
            var subscribers = await _preferenceRepo.GetDailySummarySubscribersAsync(stoppingToken);
            var scheduler = await _schedulerFactory.GetScheduler(stoppingToken);
            var count = 0;

            foreach (var pref in subscribers)
            {
                if (pref.DailySummary == null || !pref.DailySummary.Enabled)
                    continue;

                await ScheduleUserJob(scheduler, pref.UserId,
                    pref.DailySummary.Hour, pref.DailySummary.Minute, stoppingToken);
                count++;
            }

            _logger.LogInformation(
                "DailySummarySchedulerService initialized — scheduled {Count} daily summary jobs", count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize daily summary schedules");
        }
    }

    /// <summary>
    /// Reschedules (or creates) the daily summary job for a user after preference update.
    /// Called from the UpdatePreferencesHandler or a domain event.
    /// </summary>
    public async Task RescheduleAsync(string userId, int hour, int minute, CancellationToken ct = default)
    {
        var scheduler = await _schedulerFactory.GetScheduler(ct);
        var triggerKey = GetTriggerKey(userId);

        // Remove existing trigger if any
        if (await scheduler.CheckExists(triggerKey, ct))
        {
            await scheduler.UnscheduleJob(triggerKey, ct);
            _logger.LogDebug("Removed existing trigger for user {UserId}", userId);
        }

        await ScheduleUserJob(scheduler, userId, hour, minute, ct);
        _logger.LogInformation("Rescheduled daily summary for user {UserId} at {Hour}:{Minute:D2}",
            userId, hour, minute);
    }

    /// <summary>
    /// Removes the daily summary job for a user (e.g. when they disable it).
    /// </summary>
    public async Task RemoveAsync(string userId, CancellationToken ct = default)
    {
        var scheduler = await _schedulerFactory.GetScheduler(ct);
        var triggerKey = GetTriggerKey(userId);
        var jobKey = GetJobKey(userId);

        if (await scheduler.CheckExists(triggerKey, ct))
            await scheduler.UnscheduleJob(triggerKey, ct);

        if (await scheduler.CheckExists(jobKey, ct))
            await scheduler.DeleteJob(jobKey, ct);

        _logger.LogInformation("Removed daily summary schedule for user {UserId}", userId);
    }

    // ──────────────── Internal ────────────────

    private async Task ScheduleUserJob(
        IScheduler scheduler, string userId, int hour, int minute, CancellationToken ct)
    {
        var jobKey = GetJobKey(userId);
        var triggerKey = GetTriggerKey(userId);

        // Cron: "0 {min} {hour} * * ?" — fires daily at the specified time
        var cronExpression = $"0 {minute} {hour} * * ?";

        // Define the job with the user ID in the JobDataMap so the job can load preferences and know which user it's for. 
        // StoreDurably allows the job to exist without a trigger until we schedule it.
        var jobDetail = JobBuilder.Create<DailySummaryJob>()
            .WithIdentity(jobKey)
            .UsingJobData(DailySummaryJob.UserIdKey, userId)
            .StoreDurably()
            .Build();

        // Define the trigger with the Cron expression and the correct timezone. 
        // The trigger is linked to the job by the job key.
        var trigger = TriggerBuilder.Create()
            .WithIdentity(triggerKey)
            .ForJob(jobKey)
            .WithCronSchedule(cronExpression, x => x.InTimeZone(ColombiaTz))
            .Build();

        // AddJob + ScheduleJob (replace if exists)
        await scheduler.AddJob(jobDetail, replace: true, storeNonDurableWhileAwaitingScheduling: true, ct);

        if (await scheduler.CheckExists(triggerKey, ct))
        {
            await scheduler.RescheduleJob(triggerKey, trigger, ct);
        }
        else
        {
            await scheduler.ScheduleJob(trigger, ct);
        }

        _logger.LogDebug("Scheduled daily summary for user {UserId} with cron '{Cron}' (America/Bogota)",
            userId, cronExpression);
    }

    private static JobKey GetJobKey(string userId) =>
        new($"DailySummary_{userId}", "DailySummary");

    private static TriggerKey GetTriggerKey(string userId) =>
        new($"DailySummaryTrigger_{userId}", "DailySummary");
}
