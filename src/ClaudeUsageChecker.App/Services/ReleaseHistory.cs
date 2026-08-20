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
    /// <remarks>
    /// Entries written by earlier versions carry three numbers and no label;
    /// they read back unchanged, as a version without a pre-release.
    /// </remarks>
    public static ProgramVersion? Parse(string? stored) =>
        ProgramVersion.TryParse(stored, out var version) ? version : null;

    /// <summary>
    /// Writes the version down, pre-release label and all.
    /// </summary>
    /// <remarks>
    /// The label has to be recorded, otherwise 0.7.1-beta.5 and the finished
    /// 0.7.1 leave the same trace and the step between them is invisible - which
    /// is exactly how the summary went missing on the way out of a test build.
    /// </remarks>
    public static string Format(ProgramVersion version)
    {
        ArgumentNullException.ThrowIfNull(version);
        return version.ToString();
    }

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
    /// <para>
    /// Arriving at the finished version counts as a step forward even though the
    /// number has not moved: whoever tested 0.7.1-beta.5 and now runs 0.7.1 has
    /// reached the release, and the entry describing it may well have grown
    /// since the first test build. Between two test builds of the same number,
    /// by contrast, it stays quiet - the changelog has nothing new to say there,
    /// and repeating the same page at every hop is noise.
    /// </para>
    /// </remarks>
    public static bool ShouldShow(ProgramVersion? previous, ProgramVersion current, bool isFirstInstall)
    {
        ArgumentNullException.ThrowIfNull(current);

        if (previous is null)
        {
            return !isFirstInstall;
        }

        if (previous.Number != current.Number)
        {
            return previous.Number < current.Number;
        }

        return previous.IsPreRelease && !current.IsPreRelease;
    }
}
