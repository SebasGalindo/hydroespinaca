namespace BffService.Application.Helpers;

/// <summary>
/// Helper class to validate analytics request parameters.
/// Reduces code duplication across analytics services.
/// </summary>
public static class AnalyticsRequestValidator
{
    private static readonly string[] ValidViews = { "hourly", "daily", "weekly", "monthly" };

    /// <summary>
    /// Validates the view parameter and date range for analytics requests.
    /// </summary>
    /// <param name="view">The view/granularity (hourly, daily, weekly, monthly)</param>
    /// <param name="startDate">Start date of the range</param>
    /// <param name="endDate">End date of the range</param>
    /// <exception cref="InvalidOperationException">Thrown when validation fails</exception>
    public static void ValidateRequest(string view, DateTime startDate, DateTime endDate)
    {
        ValidateView(view);
        ValidateDateRange(view, startDate, endDate);
    }

    /// <summary>
    /// Validates that the view parameter is one of the allowed values.
    /// </summary>
    private static void ValidateView(string view)
    {
        var viewLower = view.ToLower();
        if (!ValidViews.Contains(viewLower))
        {
            throw new InvalidOperationException(
                $"Vista inválida '{view}'. Valores permitidos: {string.Join(", ", ValidViews)}");
        }
    }

    /// <summary>
    /// Validates that the date range is valid and within the maximum allowed for the view.
    /// </summary>
    private static void ValidateDateRange(string view, DateTime startDate, DateTime endDate)
    {
        // Validate date range - start date cannot be after end date
        if (startDate > endDate)
        {
            throw new InvalidOperationException("La fecha inicial no puede ser mayor a la fecha final.");
        }

        // Validate maximum date range based on view (date comparison only)
        var viewLower = view.ToLower();
        var daysDifference = (endDate.Date - startDate.Date).Days;
        var maxDaysAllowed = GetMaxDaysAllowedForView(viewLower);

        if (daysDifference > maxDaysAllowed)
        {
            var viewLabel = GetViewLabel(viewLower, maxDaysAllowed);
            throw new InvalidOperationException(
                $"Rango de fechas inválido para vista {viewLabel}. Días seleccionados: {daysDifference}");
        }
    }

    /// <summary>
    /// Gets the maximum number of days allowed for a given view.
    /// </summary>
    private static int GetMaxDaysAllowedForView(string viewLower) => viewLower switch
    {
        "hourly" => 1,      // Max 1 day
        "daily" => 14,      // Max 14 days
        "weekly" => 84,     // Max 12 weeks
        "monthly" => 365,   // Max 12 months
        _ => 14
    };

    /// <summary>
    /// Gets a human-readable label for the view with max days information.
    /// </summary>
    private static string GetViewLabel(string viewLower, int maxDays) => viewLower switch
    {
        "hourly" => "horaria (máximo 1 día)",
        "daily" => "diaria (máximo 14 días)",
        "weekly" => "semanal (máximo 84 días)",
        "monthly" => "mensual (máximo 365 días)",
        _ => $"{viewLower} (máximo {maxDays} días)"
    };
}
