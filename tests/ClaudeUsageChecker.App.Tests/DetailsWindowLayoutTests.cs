using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Media;
using ClaudeUsageChecker.App.Views;
using ClaudeUsageChecker.Core.Models;
using ClaudeUsageChecker.Core.Services;

namespace ClaudeUsageChecker.App.Tests;

/// <summary>
/// Checks that the content fits the window.
/// </summary>
/// <remarks>
/// The occasion was an overflow: two buttons side by side needed more room
/// than the 380 pixel window offers - the second extended past it and was only
/// half readable. Nothing like that shows in a functional test, because every
/// control is present and operable. Only measuring reveals it.
/// </remarks>
public class DetailsWindowLayoutTests
{
    [AvaloniaFact]
    public void TheUpdateNoticeFitsTheWindow()
    {
        var window = new DetailsWindow();
        window.Render(ReadyState());
        window.SetUpdateNotice(
            "Version 0.9.9 is available (installed: 0.3.0).",
            new Uri("https://example.invalid/release"),
            canInstall: true);

        Assert.True(LayoutProbe.FitsTheWidth(window, out var width),
            $"The content needs {width:0} pixels, the window is {window.Width:0} wide.");
    }

    [AvaloniaFact]
    public void TheExpiredSignInNoticeFitsToo()
    {
        var window = new DetailsWindow();
        window.Render(ReadyState());
        window.SetSignInNotice(
            "Your own sign-in has expired and was removed. "
            + "Please sign in again under Settings.");

        Assert.True(LayoutProbe.FitsTheWidth(window, out var width),
            $"The content needs {width:0} pixels, the window is {window.Width:0} wide.");
    }

    [AvaloniaFact]
    public void AllFourUsageWindowsFit()
    {
        var window = new DetailsWindow();
        var now = DateTimeOffset.UtcNow;

        window.Render(new UsageState
        {
            Kind = UsageStateKind.Ready,
            Snapshot = new UsageSnapshot
            {
                Session = new UsageWindow(100, now.AddHours(1)),
                Weekly = new UsageWindow(100, now.AddDays(3)),
                ScopedWeekly =
                [
                    new ScopedUsageWindow("Opus", new UsageWindow(100, now.AddDays(3))),
                    new ScopedUsageWindow("Fable", new UsageWindow(100, now.AddDays(3)))
                ],
                ExtraUsage = new ExtraUsage(true, 999.99m, 888.88m, 99.9),
                RetrievedAt = now
            }
        });

        Assert.True(LayoutProbe.FitsTheWidth(window, out var width),
            $"The content needs {width:0} pixels, the window is {window.Width:0} wide.");
    }

    /// <summary>
    /// An unresolvable DynamicResource stays empty in silence - the border would
    /// then be invisible without anything failing.
    /// </summary>
    [AvaloniaFact]
    public void TheBorderCarriesTheColourOfTheIcon()
    {
        var window = new DetailsWindow();
        window.Show();

        var rahmen = window.GetLogicalDescendants().OfType<Border>().First();

        Assert.NotNull(rahmen.BorderBrush);
        Assert.Equal(Color.FromRgb(0xD9, 0x77, 0x57), ((ISolidColorBrush)rahmen.BorderBrush!).Color);
        Assert.True(rahmen.BorderThickness.Top > 0);

        window.Hide();
    }

    private static UsageState ReadyState() => new()
    {
        Kind = UsageStateKind.Ready,
        Snapshot = new UsageSnapshot
        {
            Session = new UsageWindow(7, DateTimeOffset.UtcNow.AddHours(3)),
            Weekly = new UsageWindow(16, DateTimeOffset.UtcNow.AddDays(3)),
            RetrievedAt = DateTimeOffset.UtcNow
        }
    };
}
