namespace ClaudeUsageChecker.App.Settings;

/// <summary>Which releases an update check takes into account.</summary>
/// <remarks>
/// Pre-releases exist so that a version can be handed to a few testers before
/// everybody gets it. The choice is deliberately not in plain sight: it is
/// revealed by clicking the version number in the settings five times. Anyone
/// who finds that has gone looking, and anyone who has not cannot end up on a
/// half-finished build by accident.
/// </remarks>
public enum UpdateChannel
{
    /// <summary>Only published releases. The default.</summary>
    Stable,

    /// <summary>Pre-releases too, newest first.</summary>
    PreRelease
}
