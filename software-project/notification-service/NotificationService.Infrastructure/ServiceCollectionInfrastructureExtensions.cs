using HydroEspinaca.Shared.Extensions;
using HydroEspinaca.Shared.Authentication.Services;
using HydroEspinaca.Shared.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NotificationService.Domain.Interfaces;
using NotificationService.Infrastructure.Templating;
using NotificationService.Infrastructure.Transport;
using NotificationService.Infrastructure.Workers;
using NotificationService.Infrastructure.Persistence;
using NotificationService.Infrastructure.Channels;
using NotificationService.Infrastructure.DailySummary;
using Microsoft.Extensions.Options;
using NotificationService.Infrastructure.Options;
using Polly;
using Polly.Extensions.Http;
using Quartz;

namespace NotificationService.Infrastructure;

// Wiring de la capa de infraestructura: Mongo, cola/worker, templating, sanitización y persistencia.
public static class ServiceCollectionInfrastructureExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
    // Mongo (usado por IdempotencyStore y EmailLogRepository)
        services.AddMongoSettings(configuration);

    // Cola in-memory y worker que procesa el envío
        services.AddSingleton<IEmailQueue, InMemoryEmailQueue>();
        services.AddHostedService<EmailDispatcherHostedService>();

    // Templating + sanitización de HTML
        services.AddSingleton<ITemplateRenderer, FluidTemplateRenderer>();
        services.AddSingleton<ISanitizer, HtmlSanitizerAdapter>();

    // Opciones de configuración (Email, Resend y Smtp) leídas desde appsettings*
        services.Configure<EmailSettings>(configuration.GetSection("Email"));
        services.Configure<ResendSettings>(configuration.GetSection("Email:Resend"));
        services.Configure<SmtpSettings>(configuration.GetSection("Email:Smtp"));

    // Push & WhatsApp options
        services.Configure<WebPushSettings>(configuration.GetSection("Push:WebPush"));
        services.Configure<ExpoPushSettings>(configuration.GetSection("Push:Expo"));
        services.Configure<WhatsAppSettings>(configuration.GetSection("WhatsApp"));
        services.Configure<TwilioSettings>(configuration.GetSection("WhatsApp:Twilio"));

    // Service URL options (for DailySummaryDataAggregator)
        services.Configure<ServiceUrlSettings>(configuration.GetSection("Services"));

    // M2M Authentication (for service-to-service calls)
        services.Configure<M2MAuthOptions>(configuration.GetSection(M2MAuthOptions.SectionName));
        services.AddHttpClient("M2MAuth");
        services.AddTransient<M2MTokenService>(sp =>
        {
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient("M2MAuth");
            var options = sp.GetRequiredService<IOptions<M2MAuthOptions>>();
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<M2MTokenService>>();
            return new M2MTokenService(httpClient, options, logger);
        });

    // HttpClient para Resend con políticas de resiliencia básicas (reintentos exponenciales)
        var retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(3, attempt => TimeSpan.FromMilliseconds(200 * Math.Pow(2, attempt)));

        services.AddHttpClient("Resend")
            .AddPolicyHandler(retryPolicy);

    // HttpClient para Expo Push
        services.AddHttpClient("ExpoPush")
            .AddPolicyHandler(retryPolicy);

    // HttpClient para Twilio WhatsApp
        services.AddHttpClient("TwilioWhatsApp")
            .AddPolicyHandler(retryPolicy);

    // HttpClient para DailySummary aggregation
        services.AddHttpClient("DailySummary")
            .AddPolicyHandler(retryPolicy);

    // Proveedores concretos
        services.AddSingleton<ResendEmailSender>();
        services.AddSingleton<SmtpEmailSender>();

    // Enrutador compuesto: Resend primario, SMTP fallback
        services.AddSingleton<IEmailSender>(sp =>
        {
            var primary = sp.GetRequiredService<ResendEmailSender>();
            var fallback = sp.GetRequiredService<SmtpEmailSender>();
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<CompositeEmailSender>>();
            return new CompositeEmailSender(primary, fallback, logger);
        });

    // Multi-channel senders (INotificationChannel)
        services.AddSingleton<INotificationChannel, EmailChannelAdapter>();
        services.AddSingleton<INotificationChannel, ExpoPushSender>();
        services.AddSingleton<INotificationChannel, WebPushSender>();
        services.AddSingleton<INotificationChannel, TwilioWhatsAppSender>();

    // Multi-channel dispatcher
        services.AddSingleton<INotificationDispatcher, CompositeNotificationDispatcher>();

    // Persistencia: logs, idempotencia y grupos en Mongo
    services.AddSingleton<IEmailLogRepository, MongoEmailLogRepository>();
    services.AddSingleton<IIdempotencyStore, MongoIdempotencyStore>();
    services.AddSingleton<INotificationGroupRepository, NotificationGroupRepository>();

    // New multi-channel persistence
    services.AddSingleton<INotificationPreferenceRepository, MongoNotificationPreferenceRepository>();
    services.AddSingleton<IPushSubscriptionRepository, MongoPushSubscriptionRepository>();
    services.AddSingleton<INotificationLogRepository, MongoNotificationLogRepository>();

    // ──────────── Quartz.NET (Daily Summary Scheduler) ────────────
        services.AddQuartz();
        services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

    // Daily summary services
        services.AddSingleton<DailySummaryContentFormatter>();
        services.AddTransient<IDailySummaryDataAggregator, DailySummaryDataAggregator>();
        services.AddSingleton<DailySummarySchedulerService>();
        services.AddSingleton<IDailySummaryScheduler>(sp => sp.GetRequiredService<DailySummarySchedulerService>());
        services.AddHostedService(sp => sp.GetRequiredService<DailySummarySchedulerService>());

        return services;
    }
}
