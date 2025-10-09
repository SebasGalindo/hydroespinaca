using HydroEspinaca.Shared.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NotificationService.Domain.Interfaces;
using NotificationService.Infrastructure.Templating;
using NotificationService.Infrastructure.Transport;
using NotificationService.Infrastructure.Workers;
using NotificationService.Infrastructure.Persistence;
using Microsoft.Extensions.Options;
using NotificationService.Infrastructure.Options;
using Polly;
using Polly.Extensions.Http;

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

    // HttpClient para Resend con políticas de resiliencia básicas (reintentos exponenciales)
        var retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(3, attempt => TimeSpan.FromMilliseconds(200 * Math.Pow(2, attempt)));

        services.AddHttpClient("Resend")
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

    // Persistencia: logs, idempotencia y grupos en Mongo
    services.AddSingleton<IEmailLogRepository, MongoEmailLogRepository>();
    services.AddSingleton<IIdempotencyStore, MongoIdempotencyStore>();
    services.AddSingleton<INotificationGroupRepository, NotificationGroupRepository>();

        return services;
    }
}
