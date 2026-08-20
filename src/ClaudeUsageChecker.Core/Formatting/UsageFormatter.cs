using System.Text;
using ClaudeUsageChecker.Core.Localization;
using ClaudeUsageChecker.Core.Models;
using ClaudeUsageChecker.Core.Services;

namespace ClaudeUsageChecker.Core.Formatting;

/// <summary>
/// Produces the text for the tray tooltip, the context menu and the details window.
/// </summary>
/// <remarks>
/// The split follows the space available: the tooltip condenses the two most
/// important windows including the reset time, the context menu lists every
/// reported limit with its remaining time.
///
/// All building blocks come from the language file of the selected language.
/// That includes the order within a line: where English says "2 h left", another
/// language may want the parts the other way round - which is why the file holds
/// whole sentences with placeholders rather than fragments to be pieced together.
/// </remarks>
public static class UsageFormatter
{
    /// <summary>Windows truncates tray tooltips hard at 127 characters.</summary>
    public const int WindowsTooltipMaxLength = 127;

    /// <summary>
    /// Short text for the tooltip, for example
    /// "Session 33 % - resets 16:30 (2 h 14 min)".
    /// Deliberately limited to session and weekly limit, so the length cap holds.
    /// </summary>
    public static string ToTooltip(UsageState state, DateTimeOffset now)
    {
        var text = state.Kind switch
        {
            UsageStateKind.Initializing => T.TooltipLoading,
            UsageStateKind.NotConfigured => T.TooltipNotSignedIn,
            UsageStateKind.AuthenticationFailed => T.TooltipTokenExpired,
            UsageStateKind.Unavailable => T.TooltipOffline,
            _ => BuildTooltipText(state, now)
        };

        return Truncate(text, WindowsTooltipMaxLength);
    }

    private static string BuildTooltipText(UsageState state, DateTimeOffset now)
    {
        if (state.Snapshot is not { } snapshot)
        {
            return T.TooltipNoData;
        }

        var builder = new StringBuilder();
        AppendTooltipLine(builder, T.TooltipSession, snapshot.Session, now);
        AppendTooltipLine(builder, T.TooltipWeekly, snapshot.Weekly, now);

        if (builder.Length == 0)
        {
            return T.TooltipNoLimits;
        }

        if (state.Kind == UsageStateKind.Stale)
        {
            builder.Append('\n').Append(T.TooltipStale);
        }

        return builder.ToString();
    }

    private static void AppendTooltipLine(
        StringBuilder builder, string label, UsageWindow? window, DateTimeOffset now)
    {
        if (window is null)
        {
            return;
        }

        if (builder.Length > 0)
        {
            builder.Append('\n');
        }

        var remaining = window.TimeUntilReset(now);

        builder.Append(DurationFormatter.IsDue(remaining)
            ? T.TooltipLineDue(label, window.Utilization)
            : T.TooltipLine(
                label,
                window.Utilization,
                DurationFormatter.ToResetMoment(window.ResetsAt, now),
                DurationFormatter.ToCompact(remaining)));
    }

    /// <summary>
    /// The reported usage windows in a fixed order, each with its label. Windows
    /// that are not reported are left out - depending on the subscription the API
    /// may report no model-specific weekly limit at all.
    /// </summary>
    public static IEnumerable<(string Label, UsageWindow Window)> EnumerateWindows(UsageSnapshot? snapshot)
    {
        if (snapshot is null)
        {
            yield break;
        }

        if (snapshot.Session is { } session)
        {
            yield return (T.WindowSession, session);
        }

        if (snapshot.Weekly is { } weekly)
        {
            yield return (T.WindowWeeklyAll, weekly);
        }

        // The model name comes from the response and is not translated.
        foreach (var scoped in snapshot.ScopedWeekly)
        {
            yield return (T.WindowWeeklyModel(scoped.ModelName), scoped.Window);
        }
    }

    /// <summary>
    /// A line for the context menu, for example "Session (5 h): 33 % - 2 h 14 min left".
    /// Without the time of day - that one is in the tooltip.
    /// </summary>
    public static string ToMenuLine(string label, UsageWindow window, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(window);

        var remaining = window.TimeUntilReset(now);

        return DurationFormatter.IsDue(remaining)
            ? T.MenuLineDue(label, window.Utilization)
            : T.MenuLine(label, window.Utilization, DurationFormatter.ToCompact(remaining));
    }

    /// <summary>
    /// The line for extra usage, for example
    /// "Extra usage: 46 % - 22,76 EUR of 50,00 EUR".
    /// Returns null when no extra usage is active.
    /// </summary>
    public static string? ToExtraUsageLine(ExtraUsage? extraUsage)
    {
        if (extraUsage is not { IsEnabled: true })
        {
            return null;
        }

        var parts = new List<string>(2);

        if (extraUsage.Utilization is { } utilization)
        {
            parts.Add(T.Percent(utilization));
        }

        if (extraUsage is { Used: { } used, Limit: { } limit })
        {
            parts.Add(T.ExtraUsedOfLimit(Money(used, extraUsage), Money(limit, extraUsage)));
        }
        else if (extraUsage.Used is { } usedOnly)
        {
            parts.Add(T.ExtraUsedOnly(Money(usedOnly, extraUsage)));
        }

        // The API sometimes reports the quota as enabled without supplying figures.
        return parts.Count == 0 ? T.ExtraLineActive : T.ExtraLine(string.Join(" - ", parts));
    }


    /// <summary>
    /// Writes an amount with the currency and the number of places the API named
    /// for this account - EUR here, USD or BRL elsewhere.
    /// </summary>
    public static string Money(decimal amount, ExtraUsage extraUsage)
    {
        ArgumentNullException.ThrowIfNull(extraUsage);

        return MoneyFormatter.Format(amount, extraUsage.Currency, extraUsage.Decimals);
    }

    /// <summary>The label of a single row in the details window.</summary>
    public static string ToDetailLine(string label, UsageWindow? window, DateTimeOffset now)
    {
        if (window is null)
        {
            return T.NotAvailable(label);
        }

        var remaining = window.TimeUntilReset(now);
        var moment = DurationFormatter.ToResetMoment(window.ResetsAt, now);

        return DurationFormatter.IsDue(remaining)
            ? T.DetailLineDue(label, window.Utilization, moment)
            : T.DetailLine(label, window.Utilization, DurationFormatter.ToCompact(remaining), moment);
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..(maxLength - 1)] + "…";
}
