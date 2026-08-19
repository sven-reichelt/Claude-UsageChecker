using System;
using System.Text.Json.Serialization;
using ClaudeUsageChecker.Core.Localization;

namespace ClaudeUsageChecker.App.Settings;

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

    /// <summary>Warning threshold in percent.</summary>
    [JsonPropertyName("warningThreshold")]
    public double WarningThreshold { get; set; } = 75d;

    /// <summary>Critical threshold in percent.</summary>
    [JsonPropertyName("criticalThreshold")]
    public double CriticalThreshold { get; set; } = 90d;

    /// <summary>
    /// Tag of the selected language, "de" or "pt-BR" for instance. Empty means:
    /// follow the language of the system.
    /// </summary>
    [JsonPropertyName("language")]
    public string? Language { get; set; }

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
