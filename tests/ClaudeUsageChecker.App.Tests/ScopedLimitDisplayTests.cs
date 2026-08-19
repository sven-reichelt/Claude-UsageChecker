using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using ClaudeUsageChecker.App.Tray;
using ClaudeUsageChecker.App.Views;
using ClaudeUsageChecker.Core.Models;
using ClaudeUsageChecker.Core.Services;

namespace ClaudeUsageChecker.App.Tests;

/// <summary>
/// Checks that model-specific weekly limits are actually visible.
/// </summary>
/// <remarks>
/// The Fable weekly limit was missing from the display without anything
/// failing: the API reported it, the data model knew only Opus and Sonnet, and
/// the row fell away in silence. These tests follow the route all the way to the
/// interface.
/// </remarks>
public class ScopedLimitDisplayTests
{
    [Fact]
    public void TheContextMenuListsTheModelSpecificLimit()
    {
        var lines = TrayIconController.BuildStatusLines(State("Fable"), Now);

        Assert.Contains(lines, z => z.Contains("Fable", StringComparison.Ordinal));
    }

    /// <summary>
    /// The slots in the menu are created once; surplus lines would fall away
    /// without anyone noticing.
    /// </summary>
    [Fact]
    public void ThereAreEnoughSlotsForEveryReportedLimit()
    {
        var zustand = State("Fable", "Opus", "Sonnet", "Ein viertes Modell", "Ein fuenftes Modell");

        var lines = TrayIconController.BuildStatusLines(zustand, Now);

        Assert.True(lines.Count <= TrayIconController.StatusSlotCount,
            $"Das Menue haelt {TrayIconController.StatusSlotCount} Plaetze bereit, "
            + $"gebraucht werden {lines.Count}.");
    }

    [AvaloniaFact]
    public void TheDetailsWindowShowsOneRowPerReportedLimit()
    {
        var window = new DetailsWindow();

        window.Render(State("Fable"));

        // Session, weekly total and the Fable weekly limit.
        Assert.Equal(3, window.FindControl<StackPanel>("WindowsPanel")!.Children.Count);
    }

    [Fact]
    public void TheModelSpecificLimitHelpsDecideTheColour()
    {
        // Were it not included here, the icon would stay green although a limit
        // is nearly exhausted.
        var zustand = new UsageState
        {
            Kind = UsageStateKind.Ready,
            Snapshot = new UsageSnapshot
            {
                Session = new UsageWindow(5, Now.AddHours(2)),
                Weekly = new UsageWindow(10, Now.AddDays(3)),
                ScopedWeekly = [new ScopedUsageWindow("Fable", new UsageWindow(95, Now.AddDays(3)))],
                RetrievedAt = Now
            }
        };

        Assert.Equal(TrayIconSeverity.Critical, TrayIconSeverityResolver.Resolve(zustand, 75, 90));
    }

    private static readonly DateTimeOffset Now = new(2026, 8, 19, 21, 0, 0, TimeSpan.Zero);

    private static UsageState State(params string[] modelle) => new()
    {
        Kind = UsageStateKind.Ready,
        Snapshot = new UsageSnapshot
        {
            Session = new UsageWindow(6, Now.AddHours(3)),
            Weekly = new UsageWindow(18, Now.AddDays(3)),
            ScopedWeekly =
            [
                .. modelle.Select(m => new ScopedUsageWindow(m, new UsageWindow(2, Now.AddDays(3))))
            ],
            ExtraUsage = new ExtraUsage(true, 50m, 12m, 24d),
            RetrievedAt = Now
        }
    };
}
