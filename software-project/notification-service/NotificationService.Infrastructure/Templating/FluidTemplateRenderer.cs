using System.Collections.Concurrent;
using System.IO;
using Fluid;
using Microsoft.AspNetCore.Html;
using NotificationService.Domain.Interfaces;

namespace NotificationService.Infrastructure.Templating;

// Renderizador de plantillas basado en Fluid.Core.
// Lee layouts desde la carpeta "Templates" copiada al directorio de salida y
// expone una variable {{ body }} que ya viene sanitizada (HtmlSanitizer) y se
// inserta como HTML sin volver a codificar.
public class FluidTemplateRenderer : ITemplateRenderer
{
    private readonly ConcurrentDictionary<string, IFluidTemplate> _cache = new();
    private readonly TemplateOptions _options = new();

    private static string TemplatesRoot => Path.Combine(AppContext.BaseDirectory, "Templates");

    private IFluidTemplate GetOrParse(string fullPath)
    {
        fullPath = Path.GetFullPath(fullPath);
        return _cache.GetOrAdd(fullPath, key =>
        {
            var source = File.ReadAllText(key);
            var parser = new FluidParser();
            if (!parser.TryParse(source, out var template, out var errors))
            {
                throw new InvalidOperationException($"Fluid parse error in '{key}': {string.Join(", ", errors)}");
            }
            return template;
        });
    }

    public async Task<string> RenderAsync(string templateKey, string sanitizedBodyHtml, object? model = null, CancellationToken ct = default)
    {
        // Selección de layout: si no viene un templateKey válido, usa layouts/base.liquid
        var layoutRelPath = string.IsNullOrWhiteSpace(templateKey)
            ? Path.Combine("layouts", "base.liquid")
            : templateKey.EndsWith(".liquid", StringComparison.OrdinalIgnoreCase)
                ? templateKey
                : templateKey + ".liquid";

        var fullPath = Path.Combine(TemplatesRoot, layoutRelPath);
        if (!File.Exists(fullPath))
        {
            // Fallback seguro
            fullPath = Path.Combine(TemplatesRoot, "layouts", "base.liquid");
        }

        var template = GetOrParse(fullPath);

        var context = new TemplateContext(_options);
        // Variables básicas disponibles en el layout
        if (model != null)
        {
            context.SetValue("model", model);
        }
        // Usa HtmlString para evitar doble codificación (ya está sanitizado antes)
        context.SetValue("body", new HtmlString(sanitizedBodyHtml));

        using var writer = new StringWriter();
        await template.RenderAsync(writer, context);
        return writer.ToString();
    }
}
