using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using NotificationService.Infrastructure.Options;
using System.Net.Http;

namespace NotificationService.Api.HealthChecks;

// Health check simple: prueba conectividad HTTP a Resend y resuelve el host SMTP.
public class EmailProvidersHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly ResendSettings _resend;
    private readonly SmtpSettings _smtp;

    public EmailProvidersHealthCheck(IHttpClientFactory httpFactory, IOptions<ResendSettings> resend, IOptions<SmtpSettings> smtp)
    {
        _httpFactory = httpFactory;
        _resend = resend.Value;
        _smtp = smtp.Value;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var issues = new List<string>();

        // Resend base URL reachability (HEAD)
        try
        {
            var client = _httpFactory.CreateClient("Resend");
            using var req = new HttpRequestMessage(HttpMethod.Head, _resend.ApiBaseUrl.TrimEnd('/') + "/" );
            using var resp = await client.SendAsync(req, cancellationToken);
            if (!resp.IsSuccessStatusCode && (int)resp.StatusCode >= 500)
                issues.Add($"Resend base status: {(int)resp.StatusCode}");
        }
        catch (Exception ex)
        {
            issues.Add($"Resend error: {ex.Message}");
        }

        // SMTP DNS resolution (no conectar, solo resolver)
        try
        {
            var entry = await System.Net.Dns.GetHostEntryAsync(_smtp.Host);
            if (entry.AddressList.Length == 0)
                issues.Add("SMTP host sin direcciones IP");
        }
        catch (Exception ex)
        {
            issues.Add($"SMTP DNS error: {ex.Message}");
        }

        return issues.Count == 0
            ? HealthCheckResult.Healthy("Email providers OK")
            : HealthCheckResult.Degraded(string.Join("; ", issues));
    }
}
