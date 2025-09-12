using Ganss.Xss;
using NotificationService.Domain.Interfaces;

namespace NotificationService.Infrastructure.Templating;

// Adaptador hacia HtmlSanitizer usando el paquete HtmlSanitizer (namespace correcto: Ganss.Xss)
public class HtmlSanitizerAdapter : ISanitizer
{
    private readonly HtmlSanitizer _inner;

    public HtmlSanitizerAdapter()
    {
        _inner = new HtmlSanitizer();
        // Personaliza reglas si lo necesitas, por ejemplo permitir estilos controlados:
        // _inner.AllowedAttributes.Add("style");
    }

    public string Sanitize(string html) => _inner.Sanitize(html ?? string.Empty);
}
