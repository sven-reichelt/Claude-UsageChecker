using System.Globalization;
using System.Text;
using ClaudeUsageChecker.Core.Models;
using ClaudeUsageChecker.Core.Services;

namespace ClaudeUsageChecker.Core.Formatting;

/// <summary>
/// Erzeugt die Texte fuer Infobereich-Tooltip und Detailansicht.
/// </summary>
public static class UsageFormatter
{
    /// <summary>Windows kuerzt Tooltips im Infobereich hart nach 127 Zeichen.</summary>
    public const int WindowsTooltipMaxLength = 127;

    /// <summary>
    /// Kurztext fuer den Tooltip, z. B.
    /// "Sitzung 33 % - Reset in 2 Std 14 Min\nWoche 13 % - Reset in 4 Tg 3 Std".
    /// </summary>
    public static string ToTooltip(UsageState state, DateTimeOffset now, CultureInfo? culture = null)
    {
        culture ??= CultureInfo.CurrentCulture;

        var text = state.Kind switch
        {
            UsageStateKind.Initializing => "Claude UsageChecker - Daten werden geladen ...",
            UsageStateKind.NotConfigured => "Claude UsageChecker - kein Token hinterlegt",
            UsageStateKind.AuthenticationFailed => "Claude UsageChecker - Token abgelaufen",
            UsageStateKind.Unavailable => "Claude UsageChecker - keine Verbindung",
            _ => BuildUsageText(state, now, culture)
        };

        return Truncate(text, WindowsTooltipMaxLength);
    }

    private static string BuildUsageText(UsageState state, DateTimeOffset now, CultureInfo culture)
    {
        if (state.Snapshot is not { } snapshot)
        {
            return "Claude UsageChecker - keine Daten";
        }

        var builder = new StringBuilder();
        AppendWindow(builder, "Sitzung", snapshot.Session, now, culture);
        AppendWindow(builder, "Woche", snapshot.Weekly, now, culture);

        if (builder.Length == 0)
        {
            return "Claude UsageChecker - keine Limits gemeldet";
        }

        if (state.Kind == UsageStateKind.Stale)
        {
            builder.Append("\n(veraltet)");
        }

        return builder.ToString();
    }

    private static void AppendWindow(
        StringBuilder builder, string label, UsageWindow? window, DateTimeOffset now, CultureInfo culture)
    {
        if (window is null)
        {
            return;
        }

        if (builder.Length > 0)
        {
            builder.Append('\n');
        }

        builder.AppendFormat(
            culture,
            "{0} {1:0} % - Reset in {2}",
            label,
            window.Utilization,
            DurationFormatter.ToCompact(window.TimeUntilReset(now), culture));
    }

    /// <summary>Beschriftung einer einzelnen Zeile der Detailansicht.</summary>
    public static string ToDetailLine(
        string label, UsageWindow? window, DateTimeOffset now, CultureInfo? culture = null)
    {
        culture ??= CultureInfo.CurrentCulture;

        if (window is null)
        {
            return string.Format(culture, "{0}: nicht verfuegbar", label);
        }

        var resetLocal = window.ResetsAt.ToLocalTime();
        return string.Format(
            culture,
            "{0}: {1:0.#} % belegt - Reset in {2} (um {3:t})",
            label,
            window.Utilization,
            DurationFormatter.ToCompact(window.TimeUntilReset(now), culture),
            resetLocal);
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..(maxLength - 1)] + "\u2026";
}
