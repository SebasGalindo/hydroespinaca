namespace NotificationService.Infrastructure.Options;

// Configuración para el servidor SMTP.
// - Host/Port: dirección del servidor y puerto (587 típico para STARTTLS, 465 para SSL).
// - UseStartTls: si true, negocia STARTTLS (recomendado).
// - Username/Password: credenciales SMTP. En Gmail/Outlook: usa App Password.
public class SmtpSettings
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool UseStartTls { get; set; } = true;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty; // App Password para Gmail
}
