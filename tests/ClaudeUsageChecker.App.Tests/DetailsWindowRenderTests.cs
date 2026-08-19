using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using ClaudeUsageChecker.App.Views;
using ClaudeUsageChecker.Core.Localization;
using ClaudeUsageChecker.Core.Models;
using ClaudeUsageChecker.Core.Services;

namespace ClaudeUsageChecker.App.Tests;

/// <summary>Checks what the details window actually shows for each data situation.</summary>
public class DetailsWindowRenderTests
{
    [AvaloniaFact]
    public void ExtraUsageStaysHiddenWhenNotActive()
    {
        var window = new DetailsWindow();

        window.Render(StateWith(new ExtraUsage(false, 50m, 12m, 24d)));

        Assert.False(window.FindControl<Border>("ExtraUsageBorder")!.IsVisible);
    }

    [AvaloniaFact]
    public void ExtraUsageStaysHiddenWhenNotReported()
    {
        var window = new DetailsWindow();

        window.Render(StateWith(null));

        Assert.False(window.FindControl<Border>("ExtraUsageBorder")!.IsVisible);
    }

    [AvaloniaFact]
    public void ExtraUsageShowsBarAndCredits()
    {
        var window = new DetailsWindow();

        window.Render(StateWith(new ExtraUsage(true, 50m, 12m, 24d)));

        var border = window.FindControl<Border>("ExtraUsageBorder")!;
        var panel = window.FindControl<StackPanel>("ExtraUsagePanel")!;
        Assert.True(border.IsVisible);
        // Heading, progress bar and detail line.
        Assert.Equal(3, panel.Children.Count);
        Assert.Contains(panel.Children, c => c is ProgressBar { Value: 24d });
        Assert.Contains(panel.Children, c => c is TextBlock t && t.Text!.Contains("50", StringComparison.Ordinal));
    }

    [AvaloniaFact]
    public void ExtraUsageWithoutFiguresShowsOnlyTheNotice()
    {
        var window = new DetailsWindow();

        window.Render(StateWith(new ExtraUsage(true, null, null, null)));

        var panel = window.FindControl<StackPanel>("ExtraUsagePanel")!;
        Assert.True(window.FindControl<Border>("ExtraUsageBorder")!.IsVisible);
        // Without a utilization figure the bar is dropped: heading and detail line.
        Assert.Equal(2, panel.Children.Count);
        Assert.DoesNotContain(panel.Children, c => c is ProgressBar);
    }

    [AvaloniaFact]
    public void TheUpdateNoticeStaysHiddenWithoutAMessage()
    {
        var window = new DetailsWindow();

        window.SetUpdateNotice(null);

        Assert.False(window.FindControl<Border>("UpdateBorder")!.IsVisible);
    }

    [AvaloniaFact]
    public void TheUpdateNoticeShowsAMessageWithoutAButton()
    {
        var window = new DetailsWindow();

        window.SetUpdateNotice("Version 0.1.0 ist aktuell.");

        Assert.True(window.FindControl<Border>("UpdateBorder")!.IsVisible);
        Assert.False(window.FindControl<Button>("UpdateButton")!.IsVisible);
    }

    [AvaloniaFact]
    public void TheUpdateNoticeOffersTheReleasePage()
    {
        var window = new DetailsWindow();

        window.SetUpdateNotice("Version 0.2.0 ist available.", new Uri("https://example.invalid/release"));

        Assert.True(window.FindControl<Border>("UpdateBorder")!.IsVisible);
        Assert.True(window.FindControl<Button>("UpdateButton")!.IsVisible);
    }

    [AvaloniaFact]
    public void EveryReportedWindowAppears()
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
                ScopedWeekly =
                [
                    new ScopedUsageWindow("Opus", new UsageWindow(30, now.AddDays(3))),
                    new ScopedUsageWindow("Fable", new UsageWindow(40, now.AddDays(3)))
                ],
                RetrievedAt = now
            }
        });

        Assert.Equal(4, window.FindControl<StackPanel>("WindowsPanel")!.Children.Count);
    }

    /// <summary>
    /// Checks the character encoding end to end: source file, language file,
    /// runtime, interface. An encoding fault would show up here as garbled text
    /// rather than at the user.
    /// </summary>
    [AvaloniaFact]
    public void NonAsciiCharactersReachTheInterfaceUnharmed()
    {
        var window = new DetailsWindow();

        window.Render(new UsageState { Kind = UsageStateKind.NotConfigured });

        var text = window.FindControl<TextBlock>("MessageText")!.Text!;
        Assert.Contains("Settings → Sign in", text, StringComparison.Ordinal);

        // The same in German, because that is where the non-ASCII characters
        // are: umlauts and the arrow have to survive source file, language file,
        // runtime and interface alike.
        Localizer.Use(Language.Find("de")!);
        window.ApplyTexts();
        window.Render(new UsageState { Kind = UsageStateKind.NotConfigured });

        var german = window.FindControl<TextBlock>("MessageText")!.Text!;
        Assert.Contains("Einstellungen → Anmelden", german, StringComparison.Ordinal);
        Assert.Contains("Zugriffsrecht", german, StringComparison.Ordinal);

        // The classic failure: UTF-8 read as Latin-1.
        Assert.DoesNotContain("Ã", german, StringComparison.Ordinal);

        Localizer.Use(Language.Default);
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
