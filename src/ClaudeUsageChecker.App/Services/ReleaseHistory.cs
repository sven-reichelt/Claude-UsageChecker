using System;

namespace ClaudeUsageChecker.App.Services;

/// <summary>
/// Remembers which version ran last, and decides from it whether the summary of
/// changes is due.
/// </summary>
/// <remarks>
/// Kept apart from the window because this is exactly where the decisions are
/// made that can be got wrong: the very first start, a step back to an older
/// version, an unreadable entry in the settings file.
/// </remarks>
public static class ReleaseHistory
{
    /// <summary>
    /// Reads the remembered version. An unusable entry counts as none - a
    /// corrupted settings file must not disturb the start.
    /// </summary>
    public static Version? Parse(string? stored) =>
        Version.TryParse(stored, out var version) ? version : null;

    /// <summary>
    /// Cuts the version down to three parts.
    /// </summary>
    /// <remarks>
    /// Indispensable before every comparison: Assembly.GetName().Version always
    /// has four parts, while the remembered value and the changelog have three.
    /// Version counts a missing part as -1, which makes "0.6.0" smaller than
    /// "0.6.0.0" - without the cut the application would have considered itself
    /// out of date and shown the changes again on every start.
    /// </remarks>
    public static Version ThreePart(Version version)
    {
        ArgumentNullException.ThrowIfNull(version);
        return new Version(version.Major, version.Minor, Math.Max(version.Build, 0));
    }

    /// <summary>Writes the version with three parts; the fourth says nothing.</summary>
    public static string Format(Version version) => ThreePart(version).ToString(3);

    /// <summary>
    /// Whether the summary should be shown.
    /// </summary>
    /// <param name="previous">The remembered version, or null.</param>
    /// <param name="current">The running version.</param>
    /// <param name="isFirstInstall">
    /// Whether there was no settings file yet. It is the only sign of whether
    /// the application has ever run at all.
    /// </param>
    /// <remarks>
    /// <para>
    /// Only on a step forward. On the first start there is nothing to compare,
    /// and someone who deliberately starts an older version does not want to see
    /// changes they have just left behind.
    /// </para>
    /// <para>
    /// The special case: no remembered version, but an existing settings file.
    /// The application has run before, only the old version could not remember
    /// anything yet - it did not know the field. That applies to every update
    /// from a version predating this feature. Without this branch, the very
    /// version introducing the summary would show none.
    /// </para>
    /// </remarks>
    public static bool ShouldShow(Version? previous, Version current, bool isFirstInstall)
    {
        ArgumentNullException.ThrowIfNull(current);

        if (previous is null)
        {
            return !isFirstInstall;
        }

        return ThreePart(previous) < ThreePart(current);
    }
}
