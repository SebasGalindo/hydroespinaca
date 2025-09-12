namespace NotificationService.Infrastructure.Options;

// Configuración general de Email.
// - Provider: reservado por si más adelante permitimos elegir proveedor por config.
// - FromEmail/FromName: remitente por defecto que verán los destinatarios.
public class EmailSettings
{
    public string Provider { get; set; } = "Resend"; // Resend | SendGrid | Smtp (no usado aún; usamos Composite)
    public string FromEmail { get; set; } = default!;
    public string FromName { get; set; } = default!;
}
