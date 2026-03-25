using System.Text.Json;

namespace ChatbotService.Infrastructure.Extensions;

/// <summary>
/// Métodos de extensión para simplificar la extracción de valores de <see cref="JsonElement"/>.
/// </summary>
public static class JsonElementExtensions
{
    public static string GetStringOrDefault(this JsonElement element, string propertyName, string defaultValue)
    {
        return element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String
            ? prop.GetString() ?? defaultValue
            : defaultValue;
    }

    public static double GetDoubleOrDefault(this JsonElement element, double defaultValue)
    {
        return element.ValueKind == JsonValueKind.Number
            ? element.GetDouble()
            : defaultValue;
    }
}
