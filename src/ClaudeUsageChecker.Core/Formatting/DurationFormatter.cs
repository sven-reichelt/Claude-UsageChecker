using System.Globalization;

namespace ClaudeUsageChecker.Core.Formatting;

/// <summary>Wandelt Zeitspannen und Zeitpunkte in kompakte, gut lesbare Angaben um.</summary>
public static class DurationFormatter
{
    /// <summary>
    /// Formatiert eine Restzeit knapp: "4 Tg 3 Std", "2 Std 14 Min", "47 Min", "gleich".
    /// Bewusst kurz, weil der Windows-Tooltip auf 127 Zeichen begrenzt ist.
    /// </summary>
    public static string ToCompact(TimeSpan duration, CultureInfo? culture = null)
    {
        culture ??= CultureInfo.CurrentCulture;

        if (duration <= TimeSpan.Zero)
        {
            return "gleich";
        }

        if (duration.TotalDays >= 1)
        {
            var days = (int)duration.TotalDays;
            var hours = duration.Hours;
            return hours > 0
                ? string.Format(culture, "{0} Tg {1} Std", days, hours)
                : string.Format(culture, "{0} Tg", days);
        }

        if (duration.TotalHours >= 1)
        {
            var hours = (int)duration.TotalHours;
            var minutes = duration.Minutes;
            return minutes > 0
                ? string.Format(culture, "{0} Std {1} Min", hours, minutes)
                : string.Format(culture, "{0} Std", hours);
        }

        var totalMinutes = (int)Math.Ceiling(duration.TotalMinutes);
        return string.Format(culture, "{0} Min", Math.Max(totalMinutes, 1));
    }

    /// <summary>
    /// Formatiert den Reset-Zeitpunkt als Uhrzeit in der lokalen Zeitzone.
    /// </summary>
    /// <remarks>
    /// Eine blosse Uhrzeit waere fuer das Wochenlimit mehrdeutig - "03:00" sagt
    /// nicht, an welchem Tag. Deshalb kommt bei einem Reset an einem anderen Tag
    /// der Wochentag davor, ab einer Woche Abstand das Datum.
    /// </remarks>
    public static string ToResetMoment(DateTimeOffset resetsAt, DateTimeOffset now, CultureInfo? culture = null)
    {
        culture ??= CultureInfo.CurrentCulture;

        var localReset = resetsAt.ToLocalTime();
        var localNow = now.ToLocalTime();
        var dayDifference = (localReset.Date - localNow.Date).Days;

        return dayDifference switch
        {
            0 => localReset.ToString("t", culture),
            >= 1 and <= 6 => string.Format(
                culture,
                "{0} {1}",
                culture.DateTimeFormat.GetShortestDayName(localReset.DayOfWeek),
                localReset.ToString("t", culture)),
            _ => string.Format(culture, "{0} {1}", localReset.ToString("d", culture), localReset.ToString("t", culture))
        };
    }
}
