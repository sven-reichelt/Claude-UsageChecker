using ClaudeUsageChecker.Core.Models;
using ClaudeUsageChecker.Core.Services;

namespace ClaudeUsageChecker.App.Tray;

/// <summary>Visual severity of the tray icon.</summary>
public enum TrayIconSeverity
{
    Normal,
    Warning,
    Critical,
    Inactive
}

/// <summary>Derives the icon severity from the usage state.</summary>
public static class TrayIconSeverityResolver
{
    public static TrayIconSeverity Resolve(UsageState state, double warningThreshold, double criticalThreshold)
    {
        if (state.Kind is UsageStateKind.NotConfigured
            or UsageStateKind.AuthenticationFailed
            or UsageStateKind.Unavailable
            or UsageStateKind.Initializing)
        {
            return TrayIconSeverity.Inactive;
        }

        if (state.Snapshot is not { } snapshot)
        {
            return TrayIconSeverity.Inactive;
        }

        // Whichever value is tightest decides the colour - including the
        // model-specific weekly limits, whose number the API dictates.
        var peak = 0d;
        foreach (var window in snapshot.AllWindows())
        {
            if (window.Utilization > peak)
            {
                peak = window.Utilization;
            }
        }

        return peak switch
        {
            var p when p >= criticalThreshold => TrayIconSeverity.Critical,
            var p when p >= warningThreshold => TrayIconSeverity.Warning,
            _ => TrayIconSeverity.Normal
        };
    }
}
