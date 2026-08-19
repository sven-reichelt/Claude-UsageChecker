using ClaudeUsageChecker.Core.Models;
using ClaudeUsageChecker.Core.Services;

namespace ClaudeUsageChecker.App.Tray;

/// <summary>Optische Stufe des Infobereich-Symbols.</summary>
public enum TrayIconSeverity
{
    Normal,
    Warning,
    Critical,
    Inactive
}

/// <summary>Leitet aus dem Nutzungszustand die Symbolstufe ab.</summary>
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

        // Der jeweils angespannteste Wert bestimmt die Farbe.
        var peak = Max(snapshot.Session, snapshot.Weekly, snapshot.WeeklyOpus, snapshot.WeeklySonnet);

        return peak switch
        {
            var p when p >= criticalThreshold => TrayIconSeverity.Critical,
            var p when p >= warningThreshold => TrayIconSeverity.Warning,
            _ => TrayIconSeverity.Normal
        };
    }

    private static double Max(params UsageWindow?[] windows)
    {
        var peak = 0d;
        foreach (var window in windows)
        {
            if (window is not null && window.Utilization > peak)
            {
                peak = window.Utilization;
            }
        }

        return peak;
    }
}
