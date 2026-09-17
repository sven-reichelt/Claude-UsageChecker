using System;

namespace ClaudeUsageChecker.App.Services;

/// <summary>What happens with a new version found at startup.</summary>
public enum StartupUpdateAction
{
    /// <summary>Nothing to do or nothing to say.</summary>
    None,

    /// <summary>Install it without asking and restart.</summary>
    Install,

    /// <summary>Say that it is there, as before.</summary>
    Inform
}

/// <summary>
/// Decides what becomes of a new version: at startup, and when the check in the
/// background finds one.
/// </summary>
/// <remarks>
/// <para>
/// Two ways to live with updates, chosen in the settings. With automatic updates
/// on, a new version found at startup is installed without a question - the
/// moment the application starts is the one moment nobody is in the middle of
/// using it. With them off, the startup says a new version is there, as it
/// always has.
/// </para>
/// <para>
/// The check in the background runs either way, every two hours, and asks once
/// per version: install now, or be reminded tomorrow. It never installs by
/// itself - the application may be in the middle of something, and a restart
/// out of nowhere would be the one thing worse than a question.
/// </para>
/// </remarks>
public static class UpdatePolicy
{
    /// <summary>How often the background check asks GitHub.</summary>
    /// <remarks>
    /// Twelve calls a day, far below the sixty an hour GitHub allows an address
    /// without signing in - and a version published in the morning reaches the
    /// afternoon.
    /// </remarks>
    public static readonly TimeSpan BackgroundInterval = TimeSpan.FromHours(2);

    /// <summary>How long "remind me tomorrow" keeps quiet.</summary>
    public static readonly TimeSpan RemindLater = TimeSpan.FromHours(24);

    /// <summary>What to do with the result of the check at startup.</summary>
    /// <param name="result">What the check found.</param>
    /// <param name="automatic">Whether automatic updates are switched on.</param>
    /// <param name="informAtStartup">Whether the startup is to say a new version is there.</param>
    /// <param name="canInstallHere">
    /// Whether this copy can replace itself: the published package, at its
    /// installed location. A copy started from the downloads folder is offered
    /// the setup first, and a development build cannot replace itself at all.
    /// </param>
    /// <param name="busy">
    /// Whether something is waiting for the user that a restart would take away
    /// - a usage notice that asks to be confirmed, say.
    /// </param>
    public static StartupUpdateAction AtStartup(
        UpdateCheckResult result, bool automatic, bool informAtStartup, bool canInstallHere, bool busy = false)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.Status != UpdateCheckStatus.UpdateAvailable)
        {
            return StartupUpdateAction.None;
        }

        if (automatic && result.CanInstall && canInstallHere && !busy)
        {
            return StartupUpdateAction.Install;
        }

        // Automatic updates that cannot happen here still owe the user the news:
        // whoever switched them on wanted to be up to date, not to be told nothing.
        return automatic || informAtStartup ? StartupUpdateAction.Inform : StartupUpdateAction.None;
    }
}

/// <summary>
/// Remembers which version the user has already been asked about in the
/// background, and whether they asked to hear about it again tomorrow.
/// </summary>
/// <remarks>
/// Only for as long as the application runs. Whatever is decided here, the next
/// start either installs the version or says it is there - so there is nothing
/// worth carrying across a restart.
/// </remarks>
public sealed class UpdateReminder
{
    private ProgramVersion? _version;
    private DateTimeOffset? _remindAt;

    /// <summary>Whether the version is worth asking about now.</summary>
    /// <remarks>
    /// A version not seen before always is. One asked about already only once
    /// the reminder set for it has come due.
    /// </remarks>
    public bool ShouldAsk(ProgramVersion? version, DateTimeOffset now)
    {
        if (version is null)
        {
            return false;
        }

        if (version != _version)
        {
            return true;
        }

        return _remindAt is { } due && now >= due;
    }

    /// <summary>The user has been told about the version; no further reminder unless asked for.</summary>
    public void Asked(ProgramVersion version)
    {
        _version = version ?? throw new ArgumentNullException(nameof(version));
        _remindAt = null;
    }

    /// <summary>The user wants to hear about it again tomorrow.</summary>
    public void RemindTomorrow(ProgramVersion version, DateTimeOffset now)
    {
        _version = version ?? throw new ArgumentNullException(nameof(version));
        _remindAt = now + UpdatePolicy.RemindLater;
    }
}
