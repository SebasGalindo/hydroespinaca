using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Interfaces;
using NotificationService.Infrastructure.Options;

namespace NotificationService.Infrastructure.Transport;

// Implementación SMTP usando MailKit
//
// Objetivo:
// - Enviar correos mediante un servidor SMTP con STARTTLS opcional y credenciales.
// - Adjuntar archivos desde contenido base64.
// - No lanzar excepciones al exterior: devolver EmailSendResult fallido para permitir fallback.
//
// Flujo:
// 1) Construye un MimeMessage con From/To/Cc/Bcc/Subject y cuerpo HTML.
// 2) Convierte adjuntos base64 en bytes y los agrega al mensaje.
// 3) Conecta al host SMTP, negocia seguridad (StartTls si aplica), autentica si hay usuario.
// 4) Envía el mensaje y desconecta.
// 5) Captura errores y los reporta en el resultado.
public class SmtpEmailSender : IEmailSender
{
    private readonly SmtpSettings _smtp;
    private readonly EmailSettings _email;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(
        IOptions<SmtpSettings> smtpOptions,
        IOptions<EmailSettings> emailOptions,
        ILogger<SmtpEmailSender> logger)
    {
        _smtp = smtpOptions.Value;
        _email = emailOptions.Value;
        _logger = logger;
    }

    // Envía el email via SMTP
    public async Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken ct = default)
    {
    _logger.LogInformation("SMTP: attempting send to {ToCount} recipient(s). Subject: {Subject}", message.To?.Length ?? 0, message.Subject);
        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(_email.FromName, _email.FromEmail));
        void SafeAdd(InternetAddressList list, IEnumerable<string> values, string kind)
        {
            foreach (var v in values)
            {
                if (string.IsNullOrWhiteSpace(v)) continue;
                try { list.Add(MailboxAddress.Parse(v)); }
                catch (Exception ex) { _logger.LogWarning(ex, "SMTP: skipped invalid {Kind} address: {Addr}", kind, v); }
            }
        }
            SafeAdd(mime.To, message.To ?? Array.Empty<string>(), "to");
        SafeAdd(mime.Cc, message.Cc ?? Array.Empty<string>(), "cc");
        SafeAdd(mime.Bcc, message.Bcc ?? Array.Empty<string>(), "bcc");
        mime.Subject = message.Subject;

        var builder = new BodyBuilder { HtmlBody = message.HtmlBody };
        if (message.Attachments?.Count > 0)
        {
            foreach (var a in message.Attachments)
            {
                try
                {
                    var bytes = Convert.FromBase64String(a.ContentBase64);
                    builder.Attachments.Add(a.FileName, bytes, ContentType.Parse(a.ContentType));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Invalid attachment skipped: {File}", a.FileName);
                }
            }
        }
        mime.Body = builder.ToMessageBody();

        try
        {
            using var client = new SmtpClient();
            var secure = _smtp.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto;
            await client.ConnectAsync(_smtp.Host, _smtp.Port, secure, ct);
            if (!string.IsNullOrEmpty(_smtp.Username))
            {
                await client.AuthenticateAsync(_smtp.Username, _smtp.Password, ct);
            }
            var resp = await client.SendAsync(mime, ct);
            await client.DisconnectAsync(true, ct);
            // MailKit no devuelve un ID de proveedor estándar
            _logger.LogInformation("SMTP: send success.");
            return new EmailSendResult(true, "SMTP", null, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMTP exception while sending email");
            return new EmailSendResult(false, "SMTP", null, ex.Message);
        }
    }
}
