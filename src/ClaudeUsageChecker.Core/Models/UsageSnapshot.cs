namespace ClaudeUsageChecker.Core.Models;

/// <summary>
/// The complete usage state at one point in time - the result of one API call.
/// </summary>
public sealed record UsageSnapshot
{
    /// <summary>Rolling five-hour session limit.</summary>
    public required UsageWindow? Session { get; init; }

    /// <summary>Weekly limit across all models (seven days).</summary>
    public required UsageWindow? Weekly { get; init; }

    /// <summary>
    /// Further weekly limits that apply to a single model - each with the name
    /// the API reports for it.
    /// </summary>
    /// <remarks>
    /// This used to be fixed properties for Opus and Sonnet. When Anthropic
    /// moved the weekly limit to Fable, both stayed empty and the limit was
    /// missing from the display without anything failing. A list of names taken
    /// from the response accommodates every future model without a change here.
    /// </remarks>
    public IReadOnlyList<ScopedUsageWindow> ScopedWeekly { get; init; } = [];

    /// <summary>Extra usage credits, where available.</summary>
    public ExtraUsage? ExtraUsage { get; init; }

    /// <summary>Local time of the successful call.</summary>
    public required DateTimeOffset RetrievedAt { get; init; }

    /// <summary>Which source the token used came from.</summary>
    public Authentication.TokenSource TokenSource { get; init; } = Authentication.TokenSource.ClaudeCli;

    /// <summary>Every reported window in display order, without labels.</summary>
    public IEnumerable<UsageWindow> AllWindows()
    {
        if (Session is { } session)
        {
            yield return session;
        }

        if (Weekly is { } weekly)
        {
            yield return weekly;
        }

        foreach (var scoped in ScopedWeekly)
        {
            yield return scoped.Window;
        }
    }
}

/// <summary>
/// A weekly limit for one particular model. The name comes from the API
/// response and is not translated - "Fable" is Fable in every language.
/// </summary>
public sealed record ScopedUsageWindow(string ModelName, UsageWindow Window);
