namespace AuthService.Domain.Interfaces;

/// <summary>
/// Interface for rendering email templates
/// </summary>
public interface IEmailTemplateRenderer
{
    /// <summary>
    /// Renders an email template with the provided model
    /// </summary>
    /// <param name="templateName">Name of the template file (without extension)</param>
    /// <param name="model">Model object to use for template rendering</param>
    /// <returns>Rendered HTML content</returns>
    Task<string> RenderTemplateAsync(string templateName, object model);
}