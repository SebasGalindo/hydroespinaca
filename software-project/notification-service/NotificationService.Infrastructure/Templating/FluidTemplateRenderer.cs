using System.Collections.Concurrent;
using System.IO;
using Fluid;
using Microsoft.AspNetCore.Html;
using NotificationService.Domain.Interfaces;

namespace NotificationService.Infrastructure.Templating;

// Simplified template renderer using single base template.
// Renders HTML content into the base layout with sanitized body content.
public class FluidTemplateRenderer : ITemplateRenderer
{
    private readonly Lazy<IFluidTemplate> _baseTemplate;
    private readonly TemplateOptions _options = new();

    private static string TemplatesRoot => Path.Combine(AppContext.BaseDirectory, "Templates");

    public FluidTemplateRenderer()
    {
        _baseTemplate = new Lazy<IFluidTemplate>(() =>
        {
            var fullPath = Path.Combine(TemplatesRoot, "layouts", "base.liquid");
            if (!File.Exists(fullPath))
            {
                throw new InvalidOperationException($"Base template not found at: {fullPath}");
            }

            var source = File.ReadAllText(fullPath);
            var parser = new FluidParser();
            if (!parser.TryParse(source, out var template, out var errors))
            {
                throw new InvalidOperationException($"Fluid parse error in base template: {string.Join(", ", errors)}");
            }
            return template;
        });
    }

    public async Task<string> RenderAsync(string templateKey, string sanitizedBodyHtml, object? model = null, CancellationToken ct = default)
    {
        // Use the single base template (templateKey parameter ignored for simplicity)
        var template = _baseTemplate.Value;

        var context = new TemplateContext(_options);
        
        // Set model if provided
        if (model != null)
        {
            context.SetValue("model", model);
        }
        
        // Set sanitized body content (avoids double encoding)
        context.SetValue("body", new HtmlString(sanitizedBodyHtml));

        using var writer = new StringWriter();
        await template.RenderAsync(writer, context);
        return writer.ToString();
    }
}
