using ClaudeUsageChecker.Core.Models;

namespace ClaudeUsageChecker.Core.Services;

/// <summary>
/// Decides when a limit has reached a stage the user should be told about -
/// yellow, red, or used up - and makes sure each is told only once.
/// </summary>
/// <remarks>
/// <para>
/// Every limit is followed on its own. The icon takes its colour from whichever
/// limit is tightest, but a notice has to say which limit it is about and when
/// that one resets: a weekly limit already in the yellow must not swallow the
/// news that the session has just joined it.
/// </para>
/// <para>
/// Once per stage and period. A notice that has to be confirmed and came back
/// with every call would be a nuisance within the hour. What has been reported
/// is therefore remembered together with the end of its period; once that has
/// passed, the limit has started over and the next crossing counts again. The
/// marks can be handed out and taken back in, so that a restart - an autostart
/// every morning, say - does not repeat what was said the day before.
/// </para>
/// <para>
/// The mark follows the limit down as well as up. Whoever raises the thresholds
/// past the current figure has taken the limit out of the yellow, and when it
/// gets there again, that is news again.
/// </para>
/// </remarks>
public sealed class UsageAlertTracker
{
    /// <summary>How far a reported reset time may wander before it counts as a different one.</summary>
    private static readonly TimeSpan ResetTimeTolerance = TimeSpan.FromMinutes(1);

    private readonly Dictionary<string, UsageAlertMark> _marks;

    public UsageAlertTracker(IReadOnlyDictionary<string, UsageAlertMark>? marks = null)
    {
        _marks = marks is null
            ? new Dictionary<string, UsageAlertMark>(StringComparer.Ordinal)
            : new Dictionary<string, UsageAlertMark>(marks, StringComparer.Ordinal);
    }

    /// <summary>What has been reported so far, per limit - for keeping across a restart.</summary>
    public IReadOnlyDictionary<string, UsageAlertMark> Marks => _marks;

    /// <summary>
    /// Compares the state with what was known before and returns the notices
    /// that are due now. Updates the marks along the way.
    /// </summary>
    /// <param name="changed">Whether the marks changed and are worth saving.</param>
    public IReadOnlyList<UsageAlert> Evaluate(
        UsageState state, UsageAlertRules rules, DateTimeOffset now, out bool changed)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(rules);

        changed = ForgetEndedPeriods(now);

        // Only fresh figures. A stale state repeats what was already judged when
        // it was fresh, and an error state has nothing to judge at all.
        if (state.Kind != UsageStateKind.Ready || state.Snapshot is not { } snapshot)
        {
            return [];
        }

        var alerts = new List<UsageAlert>();

        foreach (var (limit, modelName, window) in Enumerate(snapshot))
        {
            // A reset that is already due leaves figures behind that belong to
            // a period that has ended; they stay until the next call. Judging
            // them would report the old period as if it were the new one.
            if (window.ResetsAt <= now)
            {
                continue;
            }

            var key = KeyFor(limit, modelName);
            var previous = _marks.TryGetValue(key, out var mark) ? mark.Level : UsageAlertLevel.Normal;
            var current = rules.LevelOf(window.Utilization);

            // The reset time arrives with fractions of a second that differ from
            // call to call. Kept up to date in memory, but not counted as a
            // change worth writing down every few minutes.
            if (mark is null || mark.Level != current
                || (mark.ResetsAt - window.ResetsAt).Duration() > ResetTimeTolerance)
            {
                changed = true;
            }

            _marks[key] = new UsageAlertMark(current, window.ResetsAt);

            if (HighestNewlyReached(previous, current, rules) is { } level)
            {
                alerts.Add(new UsageAlert(limit, modelName, level, window, rules.ThresholdOf(level)));
            }
        }

        return alerts;
    }

    /// <summary>
    /// The highest stage between the previous and the current one that the user
    /// wants to hear about, or null.
    /// </summary>
    /// <remarks>
    /// A limit may skip a stage - between two calls, or at the first call after
    /// starting. Then one notice about the highest stage is enough; two at once
    /// would only say the same thing twice. Where that stage is switched off,
    /// the next lower one that was passed stands in for it: whoever asked to
    /// hear about yellow should not miss it because the limit went straight on
    /// to red.
    /// </remarks>
    private static UsageAlertLevel? HighestNewlyReached(
        UsageAlertLevel previous, UsageAlertLevel current, UsageAlertRules rules)
    {
        for (var level = current; level > previous; level--)
        {
            if (rules.IsEnabled(level))
            {
                return level;
            }
        }

        return null;
    }

    private bool ForgetEndedPeriods(DateTimeOffset now)
    {
        var ended = _marks.Where(m => m.Value.ResetsAt <= now).Select(m => m.Key).ToList();

        foreach (var key in ended)
        {
            _marks.Remove(key);
        }

        return ended.Count > 0;
    }

    private static IEnumerable<(UsageAlertLimit Limit, string? ModelName, UsageWindow Window)> Enumerate(
        UsageSnapshot snapshot)
    {
        if (snapshot.Session is { } session)
        {
            yield return (UsageAlertLimit.Session, null, session);
        }

        if (snapshot.Weekly is { } weekly)
        {
            yield return (UsageAlertLimit.Weekly, null, weekly);
        }

        foreach (var scoped in snapshot.ScopedWeekly)
        {
            yield return (UsageAlertLimit.WeeklyModel, scoped.ModelName, scoped.Window);
        }
    }

    /// <summary>
    /// The language-independent name of a limit, as the marks are stored under.
    /// </summary>
    public static string KeyFor(UsageAlertLimit limit, string? modelName) => limit switch
    {
        UsageAlertLimit.Session => "session",
        UsageAlertLimit.Weekly => "weekly",
        _ => "weekly:" + modelName
    };
}
