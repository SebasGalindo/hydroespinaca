using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Interfaces;
using NotificationService.Infrastructure.Options;

namespace NotificationService.Infrastructure.Transport;

// Implementación del proveedor Resend (HTTP API)
//
// Objetivo (contrato simple):
// - Entrada: EmailMessage (to/cc/bcc/subject/html/attachments)
// - Salida: EmailSendResult con estado y metadatos del proveedor
// - Errores: devuelve Success=false y el mensaje de error (no lanza excepciones fuera)
//
// Flujo:
// 1) Construye el HttpRequest POST /emails con Authorization: Bearer <ApiKey>.
// 2) Mapea EmailMessage a la carga útil esperada por Resend.
// 3) Envía la solicitud; si HTTP no es 2xx, registra y retorna fallo.
// 4) Si es 2xx, parsea el Id devuelto y retorna éxito con ProviderMessageId.
// 5) Cualquier excepción se captura y se convierte a EmailSendResult fallido (para permitir fallback).
public class ResendEmailSender : IEmailSender
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly ResendSettings _resend;
    private readonly EmailSettings _email;
    private readonly ILogger<ResendEmailSender> _logger;

    public ResendEmailSender(
        IHttpClientFactory httpFactory,
        IOptions<ResendSettings> resendOptions,
        IOptions<EmailSettings> emailOptions,
        ILogger<ResendEmailSender> logger)
    {
        _httpFactory = httpFactory;
        _resend = resendOptions.Value;
        _email = emailOptions.Value;
        _logger = logger;
    }

    // Envía el email usando la API de Resend
    public async Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        var http = _httpFactory.CreateClient("Resend");
        var url = _resend.ApiBaseUrl.TrimEnd('/') + "/emails";

        using var req = new HttpRequestMessage(HttpMethod.Post, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _resend.ApiKey);

        var from = string.IsNullOrWhiteSpace(_email.FromName)
            ? _email.FromEmail
            : $"{_email.FromName} <{_email.FromEmail}>";

    // Resend acepta un objeto JSON con estas propiedades.
    // Nota: attachments se envían embebidos como base64.
    var payload = new
        {
            from,
            to = message.To,
            cc = message.Cc?.Length > 0 ? message.Cc : null,
            bcc = message.Bcc?.Length > 0 ? message.Bcc : null,
            subject = message.Subject,
            html = message.HtmlBody,
            attachments = message.Attachments?.Select(a => new
            {
                filename = a.FileName,
                content = a.ContentBase64,
                content_type = a.ContentType
            })
        };

        req.Content = JsonContent.Create(payload);

        try
        {
            var resp = await http.SendAsync(req, ct);
            if (!resp.IsSuccessStatusCode)
            {
                var error = await resp.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("Resend send failed: {Status} {Error}", resp.StatusCode, error);
                return new EmailSendResult(false, "Resend", null, $"{resp.StatusCode}: {error}");
            }

            var json = await resp.Content.ReadFromJsonAsync<ResendResponse>(cancellationToken: ct);
            return new EmailSendResult(true, "Resend", json?.Id, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Resend exception while sending email");
            return new EmailSendResult(false, "Resend", null, ex.Message);
        }
    }

    private sealed class ResendResponse
    {
        public string? Id { get; set; }
    }
}
