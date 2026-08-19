using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using ClaudeUsageChecker.App.Settings;
using ClaudeUsageChecker.App.Views;

namespace ClaudeUsageChecker.App.Tests;

/// <summary>
/// Checks that the settings window does not grow beyond the screen.
/// </summary>
/// <remarks>
/// It grows with its content and cannot be resized. Every new section makes it
/// taller - at some point it extends past the bottom of a low screen, taking the
/// save button with it. Because that would only show on somebody else's
/// hardware, there is a limit here.
/// </remarks>
public class SettingsWindowHeightTests
{
    [AvaloniaFact]
    public void TheContentSitsInAScrollableArea()
    {
        using var file = new TemporaryFile();
        var window = CreateWindow(file);

        Assert.NotNull(window.FindControl<ScrollViewer>("ContentScroller"));
    }

    [AvaloniaFact]
    public void TheHeightIsCapped()
    {
        using var file = new TemporaryFile();
        var window = CreateWindow(file);
        window.Show();

        var scroller = window.FindControl<ScrollViewer>("ContentScroller")!;

        Assert.True(double.IsFinite(scroller.MaxHeight),
            "Without a cap the window keeps growing with every further section.");
        Assert.True(scroller.MaxHeight >= 300d);

        window.Hide();
    }

    /// <summary>
    /// The limit follows the working area of the screen the window appears on.
    /// </summary>
    [AvaloniaFact]
    public void TheLimitFollowsTheScreen()
    {
        using var file = new TemporaryFile();
        var window = CreateWindow(file);
        window.Show();

        var screen = window.Screens.ScreenFromWindow(window);
        Assert.NotNull(screen);

        var available = screen!.WorkingArea.Height / screen.Scaling;
        var scroller = window.FindControl<ScrollViewer>("ContentScroller")!;

        Assert.True(scroller.MaxHeight <= available,
            $"Die Grenze liegt bei {scroller.MaxHeight:0}, der Arbeitsbereich bei {available:0}.");

        window.Hide();
    }

    /// <summary>
    /// The window stays inside the working area, not merely below its cap.
    /// </summary>
    /// <remarks>
    /// This is the test that was missing. The cap on the scroll area was in
    /// place and every test was green, while the window still hung 124 pixels
    /// below the bottom edge of the screen with the save button out of reach:
    /// Avalonia centres on opening, using the height of that moment, and the
    /// content grows downwards afterwards. A cap says nothing about position.
    /// </remarks>
    [AvaloniaFact]
    public void TheWindowStaysInsideTheWorkingArea()
    {
        using var file = new TemporaryFile();
        var window = CreateWindow(file);
        window.Show();

        var screen = window.Screens.ScreenFromWindow(window)!;
        var frameHeight = (int)Math.Ceiling((window.FrameSize ?? window.Bounds.Size).Height * screen.Scaling);
        var bottom = window.Position.Y + frameHeight;

        Assert.True(bottom <= screen.WorkingArea.Bottom,
            $"The window ends at {bottom}, the working area at {screen.WorkingArea.Bottom}.");

        window.Hide();
    }

    /// <summary>A window pushed past the bottom edge is brought back.</summary>
    /// <remarks>
    /// Deliberately shoved out of place by hand: on the screen of whoever runs
    /// the tests the window may well fit, and then the test above proves
    /// nothing. Here the fault is forced.
    /// </remarks>
    [AvaloniaFact]
    public void AWindowPushedPastTheEdgeIsMovedBack()
    {
        using var file = new TemporaryFile();
        var window = CreateWindow(file);
        window.Show();

        var screen = window.Screens.ScreenFromWindow(window)!;
        var frameHeight = (int)Math.Ceiling((window.FrameSize ?? window.Bounds.Size).Height * screen.Scaling);

        window.Position = new PixelPoint(window.Position.X, screen.WorkingArea.Bottom - (frameHeight / 2));
        ScreenFit.MoveIntoWorkingArea(window);

        Assert.True(window.Position.Y + frameHeight <= screen.WorkingArea.Bottom,
            $"The window ends at {window.Position.Y + frameHeight}, "
                + $"the working area at {screen.WorkingArea.Bottom}.");

        window.Hide();
    }

    /// <summary>
    /// A window taller than the screen sticks to the top edge rather than the
    /// bottom one.
    /// </summary>
    /// <remarks>
    /// Otherwise the title bar would end up above the screen and the window
    /// could no longer be moved by hand - worse than an overhang at the bottom.
    /// </remarks>
    [Theory]
    [InlineData(0, 1000, 0, 1200, 0)]      // taller than the screen: stays at the top
    [InlineData(40, 1040, 500, 900, 140)]  // taskbar at the top: moved to just fit
    [InlineData(0, 1000, 100, 300, 100)]   // fits already: left alone
    public void AWindowTooTallStaysReachable(
        int workingAreaTop, int workingAreaBottom, int currentTop, int frameHeight, int expected)
    {
        Assert.Equal(
            expected,
            ScreenFit.TopInsideWorkingArea(workingAreaTop, workingAreaBottom, currentTop, frameHeight));
    }

    private static SettingsWindow CreateWindow(TemporaryFile file) =>
        new(new SettingsStore(file.Path), new AppSettings(),
            applyAutostart: _ => { });

    private sealed class TemporaryFile : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(), $"cuc-test-{Guid.NewGuid():N}.json");

        public void Dispose()
        {
            if (File.Exists(Path))
            {
                File.Delete(Path);
            }
        }
    }
}
