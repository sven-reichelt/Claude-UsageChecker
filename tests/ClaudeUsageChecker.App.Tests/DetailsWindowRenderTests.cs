using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using ClaudeUsageChecker.App.Views;
using ClaudeUsageChecker.Core.Models;
using ClaudeUsageChecker.Core.Services;

namespace ClaudeUsageChecker.App.Tests;

/// <summary>Prueft, was die Detailansicht je nach Datenlage tatsaechlich anzeigt.</summary>
public class DetailsWindowRenderTests
{
    [AvaloniaFact]
    public void ZusatzkontingentBleibtVerborgenWennNichtAktiv()
    {
        var window = new DetailsWindow();

        window.Render(StateWith(new ExtraUsage(false, 50m, 12m, 24d)));

        Assert.False(window.FindControl<Border>("ExtraUsageBorder")!.IsVisible);
    }

    [AvaloniaFact]
    public void ZusatzkontingentBleibtVerborgenWennNichtGemeldet()
    {
        var window = new DetailsWindow();

        window.Render(StateWith(null));

        Assert.False(window.FindControl<Border>("ExtraUsageBorder")!.IsVisible);
    }

    [AvaloniaFact]
    public void ZusatzkontingentZeigtBalkenUndCredits()
    {
        var window = new DetailsWindow();

        window.Render(StateWith(new ExtraUsage(true, 50m, 12m, 24d)));

        var border = window.FindControl<Border>("ExtraUsageBorder")!;
        var panel = window.FindControl<StackPanel>("ExtraUsagePanel")!;
        Assert.True(border.IsVisible);
        // Ueberschrift, Fortschrittsbalken und Detailzeile.
        Assert.Equal(3, panel.Children.Count);
        Assert.Contains(panel.Children, c => c is ProgressBar { Value: 24d });
        Assert.Contains(panel.Children, c => c is TextBlock t && t.Text!.Contains("50", StringComparison.Ordinal));
    }

    [AvaloniaFact]
    public void ZusatzkontingentOhneZahlenZeigtNurDenHinweis()
    {
        var window = new DetailsWindow();

        window.Render(StateWith(new ExtraUsage(true, null, null, null)));

        var panel = window.FindControl<StackPanel>("ExtraUsagePanel")!;
        Assert.True(window.FindControl<Border>("ExtraUsageBorder")!.IsVisible);
        // Ohne Auslastung entfaellt der Balken: Ueberschrift und Detailzeile.
        Assert.Equal(2, panel.Children.Count);
        Assert.DoesNotContain(panel.Children, c => c is ProgressBar);
    }

    [AvaloniaFact]
    public void UpdateHinweisBleibtOhneNachrichtVerborgen()
    {
        var window = new DetailsWindow();

        window.SetUpdateNotice(null);

        Assert.False(window.FindControl<Border>("UpdateBorder")!.IsVisible);
    }

    [AvaloniaFact]
    public void UpdateHinweisZeigtNachrichtOhneSchaltflaeche()
    {
        var window = new DetailsWindow();

        window.SetUpdateNotice("Version 0.1.0 ist aktuell.");

        Assert.True(window.FindControl<Border>("UpdateBorder")!.IsVisible);
        Assert.False(window.FindControl<Button>("UpdateButton")!.IsVisible);
    }

    [AvaloniaFact]
    public void UpdateHinweisBietetDieReleaseSeiteAn()
    {
        var window = new DetailsWindow();

        window.SetUpdateNotice("Version 0.2.0 ist verfuegbar.", new Uri("https://example.invalid/release"));

        Assert.True(window.FindControl<Border>("UpdateBorder")!.IsVisible);
        Assert.True(window.FindControl<Button>("UpdateButton")!.IsVisible);
    }

    [AvaloniaFact]
    public void AlleGemeldetenFensterErscheinen()
    {
        var window = new DetailsWindow();
        var now = DateTimeOffset.UtcNow;

        window.Render(new UsageState
        {
            Kind = UsageStateKind.Ready,
            Snapshot = new UsageSnapshot
            {
                Session = new UsageWindow(10, now.AddHours(1)),
                Weekly = new UsageWindow(20, now.AddDays(3)),
                WeeklyOpus = new UsageWindow(30, now.AddDays(3)),
                WeeklySonnet = new UsageWindow(40, now.AddDays(3)),
                RetrievedAt = now
            }
        });

        Assert.Equal(4, window.FindControl<StackPanel>("WindowsPanel")!.Children.Count);
    }

    /// <summary>
    /// Prueft die Zeichenkodierung durchgehend: Quelltext, Uebersetzung,
    /// Laufzeit, Oberflaeche. Ein Kodierungsfehler wuerde hier als Zeichensalat
    /// auffallen statt erst beim Nutzer.
    /// </summary>
    [AvaloniaFact]
    public void UmlauteErreichenDieOberflaecheUnbeschadet()
    {
        var window = new DetailsWindow();

        window.Render(new UsageState { Kind = UsageStateKind.NotConfigured });

        var text = window.FindControl<TextBlock>("MessageText")!.Text!;
        Assert.Contains("Einstellungen → Anmelden", text, StringComparison.Ordinal);
        Assert.Contains("Zugriffsrecht", text, StringComparison.Ordinal);
        // Der klassische Fehlerfall: UTF-8 als Latin-1 gelesen.
        Assert.DoesNotContain("Ã", text, StringComparison.Ordinal);
    }

    private static UsageState StateWith(ExtraUsage? extraUsage) => new()
    {
        Kind = UsageStateKind.Ready,
        Snapshot = new UsageSnapshot
        {
            Session = new UsageWindow(19, DateTimeOffset.UtcNow.AddHours(2)),
            Weekly = new UsageWindow(14, DateTimeOffset.UtcNow.AddDays(3)),
            ExtraUsage = extraUsage,
            RetrievedAt = DateTimeOffset.UtcNow
        }
    };
}
