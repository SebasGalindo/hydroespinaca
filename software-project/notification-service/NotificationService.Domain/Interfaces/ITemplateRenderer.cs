namespace NotificationService.Domain.Interfaces;

// Motor de plantillas: renderiza el HTML final a partir de un layout clave y el body sanitizado
public interface ITemplateRenderer
{
    Task<string> RenderAsync(string templateKey, string sanitizedBodyHtml, object? model = null, CancellationToken ct = default);
}
