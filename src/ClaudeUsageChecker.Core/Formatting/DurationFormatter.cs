using System.Globalization;
using ClaudeUsageChecker.Core.Localization;

namespace ClaudeUsageChecker.Core.Formatting;

/// <summary>Turns durations and moments into compact, readable text.</summary>
public static class DurationFormatter
{
    /// <summary>Whether the reset of a window has already fallen due.</summary>
    /// <remarks>
    /// The figures then belong to a window that has run out, and stay that way
    /// until the next call - up to the polling interval. Sentences about a
    /// remaining time do not fit that case, which is why the callers pick a
    /// different one.
    /// </remarks>
    public static bool IsDue(TimeSpan remaining) => remaining <= TimeSpan.Zero;

    /// <summary>
    /// Formats a remaining time tersely: "4 d 3 h", "2 h 14 min", "47 min", "now".
    /// Deliberately short, because the Windows tooltip is capped at 127 characters.
    /// </summary>
    /// <remarks>
    /// No culture parameter: the building blocks ("d", "h") come from the
    /// language file, and the numbers are formatted by the <see cref="Localizer"/>
    /// in the culture of the selected language. A parameter that could influence
    /// neither would be a trap - it would look like a knob without being one.
    /// </remarks>
    public static string ToCompact(TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero)
        {
            return T.DurationNow;
        }

        if (duration.TotalDays >= 1)
        {
            var days = (int)duration.TotalDays;
            var hours = duration.Hours;
            return hours > 0 ? T.DurationDaysHours(days, hours) : T.DurationDays(days);
        }

        if (duration.TotalHours >= 1)
        {
            var hours = (int)duration.TotalHours;
            var minutes = duration.Minutes;
            return minutes > 0 ? T.DurationHoursMinutes(hours, minutes) : T.DurationHours(hours);
        }

        var totalMinutes = (int)Math.Ceiling(duration.TotalMinutes);
        return T.DurationMinutes(Math.Max(totalMinutes, 1));
    }

    /// <summary>
    /// Formats the reset moment as a time of day in the local time zone.
    /// </summary>
    /// <remarks>
    /// A bare time of day would be ambiguous for the weekly limit - "03:00" does
    /// not say which day. A reset on another day is therefore preceded by the
    /// weekday, and from a week away by the date.
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
            // The abbreviated name on purpose, not the shortest one: in German
            // the latter is a single letter and therefore ambiguous - "S" would
            // stand for Samstag as well as for Sonntag.
            >= 1 and <= 6 => string.Format(
                culture,
                "{0} {1}",
                culture.DateTimeFormat.GetAbbreviatedDayName(localReset.DayOfWeek),
                localReset.ToString("t", culture)),
            _ => string.Format(culture, "{0} {1}", localReset.ToString("d", culture), localReset.ToString("t", culture))
        };
    }
}
