using AuthService.Domain.Interfaces;
using DotLiquid;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace AuthService.Infrastructure.Services;

/// <summary>
/// Liquid template renderer for email templates
/// </summary>
public class LiquidEmailTemplateRenderer : IEmailTemplateRenderer
{
    private readonly ILogger<LiquidEmailTemplateRenderer> _logger;

    public LiquidEmailTemplateRenderer(ILogger<LiquidEmailTemplateRenderer> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Renders an email template with the provided model
    /// </summary>
    /// <param name="templateName">Name of the template file (without extension)</param>
    /// <param name="model">Model object to use for template rendering</param>
    /// <returns>Rendered HTML content</returns>
    public async Task<string> RenderTemplateAsync(string templateName, object model)
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = $"AuthService.Infrastructure.Templates.{templateName}.liquid";
            
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                _logger.LogError("Embedded template resource not found: {ResourceName}", resourceName);
                throw new FileNotFoundException($"Embedded template resource not found: {resourceName}");
            }

            using var reader = new StreamReader(stream);
            var templateContent = await reader.ReadToEndAsync();
            var template = Template.Parse(templateContent);
            
            // Convert model to Hash for DotLiquid
            var hash = Hash.FromAnonymousObject(model);
            var rendered = template.Render(hash);

            _logger.LogDebug("Successfully rendered template {TemplateName}", templateName);
            return rendered;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rendering template {TemplateName}", templateName);
            throw;
        }
    }
}