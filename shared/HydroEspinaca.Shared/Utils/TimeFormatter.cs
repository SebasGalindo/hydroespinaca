namespace HydroEspinaca.Shared.Utils;

public static class TimeFormatter
{
    public static string FormatInactivity(TimeSpan duration)
    {
        if (duration.TotalMinutes < 60)
            return $"{duration.TotalMinutes:F1} minutos";

        if (duration.TotalHours < 24)
            return $"{(int)duration.TotalHours} horas y {duration.Minutes} minutos";

        return $"{duration.Days} días, {duration.Hours} horas y {duration.Minutes} minutos";
    }
}
