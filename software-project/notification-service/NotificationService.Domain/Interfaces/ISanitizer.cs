namespace NotificationService.Domain.Interfaces;

// Abstracción de sanitización para evitar dependencias de terceros en Application/Domain
public interface ISanitizer
{
    string Sanitize(string html);
}
