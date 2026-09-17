using System;
using System.Text.Json.Serialization;
using ClaudeUsageChecker.Core.Localization;
using ClaudeUsageChecker.Core.Services;

namespace ClaudeUsageChecker.App.Settings;

/// <summary>How a usage notice behaves once it is open.</summary>
/// <param name="RequiresAcknowledgement">Stays until "OK, got it!" is clicked.</param>
/// <param name="StaysOnTop">Stays in front of every other window.</param>
/// <param name="AutoClose">When it closes by itself, where no confirmation is required.</param>
public sealed record UsageAlertBehaviour(bool RequiresAcknowledgement, bool StaysOnTop, TimeSpan AutoClose);

/// <summary>
/// User settings. Deliberately holds no secrets at all - the token lives only
/// in the secret store of the operating system.
/// </summary>
public sealed class AppSettings
{
    /// <summary>Polling interval in seconds. Raised to at least 180.</summary>
    [JsonPropertyName("pollIntervalSeconds")]
    public int PollIntervalSeconds { get; set; } = 300;

    /// <summary>Start the application with Windows.</summary>
    [JsonPropertyName("launchAtLogin")]
    public bool LaunchAtLogin { get; set; }

    /// <summary>
    /// Whether the permanent setup has been offered already. The question is
    /// meant to come exactly once - anyone who declines does not want to be asked
    /// again on every start.
    /// </summary>
    [JsonPropertyName("installPromptShown")]
    public bool InstallPromptShown { get; set; }

    /// <summary>Check for updates automatically at startup.</summary>
    [JsonPropertyName("checkForUpdates")]
    public bool CheckForUpdates { get; set; } = true;

    /// <summary>
    /// Install a new version found at startup without asking, and restart.
    /// </summary>
    /// <remarks>
    /// On by default - an application nobody has to look after is one that stays
    /// up to date. Switched off, the startup says a new version is there, as it
    /// did before this setting existed. The check in the background every two
    /// hours asks either way and never installs by itself; see
    /// <see cref="Services.UpdatePolicy"/>.
    /// </remarks>
    [JsonPropertyName("autoUpdate")]
    public bool AutoUpdate { get; set; } = true;

    /// <summary>
    /// Whether the refresh button in the details window looks for a new version
    /// along the way.
    /// </summary>
    /// <remarks>
    /// On by default: whoever presses refresh wants to know where they stand,
    /// and the version is part of that. It can be switched off, because it turns
    /// one press into two calls - one to Anthropic and one to GitHub.
    /// </remarks>
    [JsonPropertyName("refreshChecksForUpdates")]
    public bool RefreshChecksForUpdates { get; set; } = true;

    /// <summary>
    /// Light, dark, or the system. Stored as text so that the settings file
    /// stays readable and an unknown value falls back to the system rather than
    /// landing somewhere by its ordinal.
    /// </summary>
    [JsonPropertyName("appearance")]
    public string? Appearance { get; set; }

    /// <summary>The choice, or the system where nothing sensible is stored.</summary>
    [JsonIgnore]
    public AppearanceMode AppearanceMode
    {
        get => AppearanceModes.Parse(Appearance);
        set => Appearance = AppearanceModes.Format(value);
    }

    /// <summary>Warning threshold in percent.</summary>
    [JsonPropertyName("warningThreshold")]
    public double WarningThreshold { get; set; } = 75d;

    /// <summary>Critical threshold in percent.</summary>
    [JsonPropertyName("criticalThreshold")]
    public double CriticalThreshold { get; set; } = 90d;

    /// <summary>Show a notice when a limit reaches the warning threshold.</summary>
    [JsonPropertyName("alertOnWarning")]
    public bool AlertOnWarning { get; set; } = true;

    /// <summary>Show a notice when a limit reaches the critical threshold.</summary>
    [JsonPropertyName("alertOnCritical")]
    public bool AlertOnCritical { get; set; } = true;

    /// <summary>Show a notice when a limit is used up.</summary>
    [JsonPropertyName("alertOnExhausted")]
    public bool AlertOnExhausted { get; set; } = true;

    /// <summary>
    /// Whether a notice stays until it is confirmed. Otherwise it closes by itself
    /// after <see cref="AlertAutoCloseSeconds"/>.
    /// </summary>
    [JsonPropertyName("alertRequiresAcknowledgement")]
    public bool AlertRequiresAcknowledgement { get; set; } = true;

    /// <summary>Whether a notice stays in front of every other window while it is open.</summary>
    [JsonPropertyName("alertStaysOnTop")]
    public bool AlertStaysOnTop { get; set; } = true;

    /// <summary>After how many seconds a notice that needs no confirmation closes.</summary>
    [JsonPropertyName("alertAutoCloseSeconds")]
    public int AlertAutoCloseSeconds { get; set; } = 30;

    /// <summary>Shortest and longest time a notice may close by itself after, in seconds.</summary>
    public const int MinimumAlertAutoCloseSeconds = 5;
    public const int MaximumAlertAutoCloseSeconds = 3600;

    /// <summary>When a notice is due - the thresholds of the icon and the stages chosen.</summary>
    /// <remarks>
    /// The same thresholds as the icon, deliberately without a second pair: a
    /// notice that said "red" while the icon was still yellow would contradict
    /// the one thing the user already sees.
    /// </remarks>
    [JsonIgnore]
    public UsageAlertRules AlertRules =>
        new(WarningThreshold, CriticalThreshold, AlertOnWarning, AlertOnCritical, AlertOnExhausted);

    /// <summary>How the notice behaves once it is open.</summary>
    [JsonIgnore]
    public UsageAlertBehaviour AlertBehaviour => new(
        AlertRequiresAcknowledgement,
        AlertStaysOnTop,
        TimeSpan.FromSeconds(Math.Clamp(
            AlertAutoCloseSeconds, MinimumAlertAutoCloseSeconds, MaximumAlertAutoCloseSeconds)));

    /// <summary>
    /// Tag of the selected language, "de" or "pt-BR" for instance. Empty means:
    /// follow the language of the system.
    /// </summary>
    [JsonPropertyName("language")]
    public string? Language { get; set; }

    /// <summary>
    /// Which releases the update check considers: only the published ones, or
    /// pre-releases as well.
    /// </summary>
    /// <remarks>
    /// Stored as text rather than as a number, so that the settings file stays
    /// readable and an unknown value falls back to the safe side instead of
    /// landing somewhere by its ordinal.
    /// </remarks>
    [JsonPropertyName("updateChannel")]
    public string? UpdateChannel { get; set; }

    /// <summary>The channel, or the published releases where nothing sensible is stored.</summary>
    [JsonIgnore]
    public UpdateChannel Channel
    {
        get => string.Equals(UpdateChannel, "prerelease", StringComparison.OrdinalIgnoreCase)
            ? Settings.UpdateChannel.PreRelease
            : Settings.UpdateChannel.Stable;
        set => UpdateChannel = value == Settings.UpdateChannel.PreRelease ? "prerelease" : "stable";
    }

    /// <summary>
    /// The version that ran last - three parts, "0.5.0" for instance. It is how
    /// the application recognises after an update which changes it has to show.
    /// Empty means: the very first start.
    /// </summary>
    [JsonPropertyName("lastRunVersion")]
    public string? LastRunVersion { get; set; }

    /// <remarks>
    /// Not serialised: the value is computed from <see cref="PollIntervalSeconds"/>.
    /// Without this attribute it ended up in the settings file as
    /// "PollInterval": "00:05:00" - never read from there, but looking like a
    /// second, possibly contradictory statement.
    /// </remarks>
    [JsonIgnore]
    public TimeSpan PollInterval => TimeSpan.FromSeconds(Math.Max(PollIntervalSeconds, 180));

    /// <summary>Smallest permissible threshold in percent.</summary>
    public const double MinimumThreshold = 5d;

    /// <summary>
    /// Checks the two thresholds and returns the reason on failure, otherwise
    /// null.
    /// </summary>
    /// <remarks>
    /// Here rather than in the window on purpose: a warning threshold above the
    /// critical one would never take effect - the icon would jump straight to
    /// red. The rule belongs to the settings, not to their presentation.
    /// </remarks>
    public static string? ValidateThresholds(double warning, double critical)
    {
        if (warning < MinimumThreshold || critical < MinimumThreshold)
        {
            return T.ThresholdTooSmall(MinimumThreshold);
        }

        if (warning > 100d || critical > 100d)
        {
            return T.ThresholdTooLarge;
        }

        if (warning >= critical)
        {
            return T.ThresholdOrder;
        }

        return null;
    }
}
