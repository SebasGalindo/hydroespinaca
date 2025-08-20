namespace NotificationService.Infrastructure.Options;

// Configuración específica del proveedor Resend.
// - ApiBaseUrl: normalmente https://api.resend.com
// - ApiKey: clave secreta de Resend obtenida en el panel (mantener en secreto / usar variables de entorno en producción)
public class ResendSettings
{
    public string ApiBaseUrl { get; set; } = "https://api.resend.com";
    public string ApiKey { get; set; } = string.Empty;
}
