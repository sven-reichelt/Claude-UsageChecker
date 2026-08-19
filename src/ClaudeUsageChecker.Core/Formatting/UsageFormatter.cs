using System.Globalization;
using System.Text;
using ClaudeUsageChecker.Core.Models;
using ClaudeUsageChecker.Core.Services;

namespace ClaudeUsageChecker.Core.Formatting;

/// <summary>
/// Erzeugt die Texte fuer Infobereich-Tooltip, Kontextmenue und Detailansicht.
/// </summary>
/// <remarks>
/// Die Aufteilung folgt dem verfuegbaren Platz: Der Tooltip fasst die beiden
/// wichtigsten Fenster samt Reset-Uhrzeit zusammen, das Kontextmenue listet
/// alle gemeldeten Limits mit Restzeit auf.
/// </remarks>
public static class UsageFormatter
{
    /// <summary>Windows kuerzt Tooltips im Infobereich hart nach 127 Zeichen.</summary>
    public const int WindowsTooltipMaxLength = 127;

    /// <summary>
    /// Kurztext fuer den Tooltip, z. B.
    /// "Sitzung 33 % - Reset 16:30 (2 Std 14 Min)".
    /// Zeigt bewusst nur Sitzung und Wochenlimit, damit die Laengenbegrenzung haelt.
    /// </summary>
    public static string ToTooltip(UsageState state, DateTimeOffset now, CultureInfo? culture = null)
    {
        culture ??= CultureInfo.CurrentCulture;

        var text = state.Kind switch
        {
            UsageStateKind.Initializing => "Claude UsageChecker - Daten werden geladen ...",
            UsageStateKind.NotConfigured => "Claude UsageChecker - nicht angemeldet",
            UsageStateKind.AuthenticationFailed => "Claude UsageChecker - Token abgelaufen",
            UsageStateKind.Unavailable => "Claude UsageChecker - keine Verbindung",
            _ => BuildTooltipText(state, now, culture)
        };

        return Truncate(text, WindowsTooltipMaxLength);
    }

    private static string BuildTooltipText(UsageState state, DateTimeOffset now, CultureInfo culture)
    {
        if (state.Snapshot is not { } snapshot)
        {
            return "Claude UsageChecker - keine Daten";
        }

        var builder = new StringBuilder();
        AppendTooltipLine(builder, "Sitzung", snapshot.Session, now, culture);
        AppendTooltipLine(builder, "Woche", snapshot.Weekly, now, culture);

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

    private static void AppendTooltipLine(
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
            "{0} {1:0} % - Reset {2} ({3})",
            label,
            window.Utilization,
            DurationFormatter.ToResetMoment(window.ResetsAt, now, culture),
            DurationFormatter.ToCompact(window.TimeUntilReset(now), culture));
    }

    /// <summary>
    /// Die gemeldeten Nutzungsfenster in fester Reihenfolge, jeweils mit Beschriftung.
    /// Nicht gemeldete Fenster entfallen - je nach Abonnement liefert die API etwa
    /// kein Opus-Wochenlimit.
    /// </summary>
    public static IEnumerable<(string Label, UsageWindow Window)> EnumerateWindows(UsageSnapshot? snapshot)
    {
        if (snapshot is null)
        {
            yield break;
        }

        if (snapshot.Session is { } session)
        {
            yield return ("Sitzung (5 Std)", session);
        }

        if (snapshot.Weekly is { } weekly)
        {
            yield return ("Woche gesamt", weekly);
        }

        if (snapshot.WeeklyOpus is { } opus)
        {
            yield return ("Woche Opus", opus);
        }

        if (snapshot.WeeklySonnet is { } sonnet)
        {
            yield return ("Woche Sonnet", sonnet);
        }
    }

    /// <summary>
    /// Zeile fuer das Kontextmenue, z. B. "Sitzung (5 Std): 33 % - noch 2 Std 14 Min".
    /// Ohne Uhrzeit - die steht im Tooltip.
    /// </summary>
    public static string ToMenuLine(
        string label, UsageWindow window, DateTimeOffset now, CultureInfo? culture = null)
    {
        ArgumentNullException.ThrowIfNull(window);
        culture ??= CultureInfo.CurrentCulture;

        return string.Format(
            culture,
            "{0}: {1:0.#} % - noch {2}",
            label,
            window.Utilization,
            DurationFormatter.ToCompact(window.TimeUntilReset(now), culture));
    }

    /// <summary>
    /// Zeile fuer das Zusatzkontingent, z. B.
    /// "Zusatzkontingent: 24 % - 12,00 von 50,00 Credits".
    /// Liefert null, wenn kein Zusatzkontingent aktiv ist.
    /// </summary>
    public static string? ToExtraUsageLine(ExtraUsage? extraUsage, CultureInfo? culture = null)
    {
        if (extraUsage is not { IsEnabled: true })
        {
            return null;
        }

        culture ??= CultureInfo.CurrentCulture;

        var parts = new List<string>(2);

        if (extraUsage.Utilization is { } utilization)
        {
            parts.Add(string.Format(culture, "{0:0.#} %", utilization));
        }

        if (extraUsage is { UsedCredits: { } used, MonthlyLimit: { } limit })
        {
            parts.Add(string.Format(culture, "{0:0.00} von {1:0.00} Credits", used, limit));
        }
        else if (extraUsage.UsedCredits is { } usedOnly)
        {
            parts.Add(string.Format(culture, "{0:0.00} Credits verbraucht", usedOnly));
        }

        // Die API meldet das Kontingent mitunter als aktiv, ohne Zahlen zu liefern.
        return parts.Count == 0
            ? "Zusatzkontingent: aktiv"
            : "Zusatzkontingent: " + string.Join(" - ", parts);
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

        return string.Format(
            culture,
            "{0}: {1:0.#} % belegt - Reset in {2} (um {3})",
            label,
            window.Utilization,
            DurationFormatter.ToCompact(window.TimeUntilReset(now), culture),
            DurationFormatter.ToResetMoment(window.ResetsAt, now, culture));
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..(maxLength - 1)] + "…";
}
