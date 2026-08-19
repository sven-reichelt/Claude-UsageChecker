using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using ClaudeUsageChecker.App.Services;
using ClaudeUsageChecker.App.Views;
using ClaudeUsageChecker.Core.Release;

namespace ClaudeUsageChecker.App.Tests;

/// <summary>
/// Checks how the changes are presented.
/// </summary>
public class ReleaseNotesWindowTests
{
    [AvaloniaFact]
    public void CanBeCreated()
    {
        var window = new ReleaseNotesWindow();

        Assert.NotNull(window.FindControl<StackPanel>("NotesPanel"));
        Assert.NotNull(window.FindControl<TextBlock>("HeadlineText"));
        Assert.NotNull(window.FindControl<Button>("CloseButton"));
    }

    [AvaloniaFact]
    public void NamesTheNewVersionInTheHeading()
    {
        var window = new ReleaseNotesWindow();

        window.Render([Release(0, 6, 0)], new Version(0, 5, 0));

        Assert.Equal("New in version 0.6.0", window.FindControl<TextBlock>("HeadlineText")!.Text);
        Assert.Contains("0.5.0", window.FindControl<TextBlock>("SubtitleText")!.Text!,
            StringComparison.Ordinal);
    }

    [AvaloniaFact]
    public void NamesTheCountForSeveralSkippedVersions()
    {
        // Someone who skips two versions should see both entries - otherwise
        // whatever the intermediate version said falls by the wayside.
        var window = new ReleaseNotesWindow();

        window.Render([Release(0, 7, 0), Release(0, 6, 0)], new Version(0, 5, 0));

        Assert.Contains("0.7.0", window.FindControl<TextBlock>("HeadlineText")!.Text!,
            StringComparison.Ordinal);
        Assert.Equal(2, window.FindControl<StackPanel>("NotesPanel")!.Children.Count);
    }

    [AvaloniaFact]
    public void CopesWithAnEmptyChangelog()
    {
        var window = new ReleaseNotesWindow();

        window.Render([]);

        Assert.Equal("No changes", window.FindControl<TextBlock>("HeadlineText")!.Text);
        Assert.Empty(window.FindControl<StackPanel>("NotesPanel")!.Children);
    }

    [AvaloniaFact]
    public void RenderingAgainAccumulatesNothing()
    {
        var window = new ReleaseNotesWindow();

        window.Render([Release(0, 6, 0)]);
        window.Render([Release(0, 6, 0)]);

        Assert.Single(window.FindControl<StackPanel>("NotesPanel")!.Children);
    }

    [AvaloniaFact]
    public void TheContentFitsTheWindow()
    {
        var window = new ReleaseNotesWindow();

        window.Render([new ReleaseNotes
        {
            Version = new Version(0, 6, 0),
            Date = new DateOnly(2026, 8, 19),
            Sections =
            [
                new ReleaseNoteSection
                {
                    Title = "Changed",
                    Entries =
                    [
                        new ReleaseNoteEntry(
                            "A very long entry that runs over several lines and is meant to "
                            + "zeigen soll, dass umbrochener Text im CreateWindow bleibt und nicht "
                            + "extend past its right edge."),
                        new ReleaseNoteEntry("An indented follow-up paragraph to it.", IsContinuation: true)
                    ]
                }
            ]
        }], new Version(0, 5, 0));

        Assert.True(LayoutProbe.FitsTheWidth(window, out var width),
            $"The content needs {width:0} pixels, the window is {window.Width:0} wide.");
    }

    /// <summary>
    /// The whole bundled changelog at once - the way the about window shows it.
    /// With some twenty versions an overflow would otherwise only show in use.
    /// </summary>
    [AvaloniaFact]
    public void TheCompleteChangelogFitsTheWindowToo()
    {
        var window = new ReleaseNotesWindow();

        window.Render(ChangelogResource.All());

        Assert.True(LayoutProbe.FitsTheWidth(window, out var width),
            $"The content needs {width:0} pixels, the window is {window.Width:0} wide.");
    }

    /// <summary>
    /// The window keeps the scroll limit set in its XAML.
    /// </summary>
    /// <remarks>
    /// The safeguard against a window growing past the screen once overwrote
    /// that limit with the height of the working area - and thereby raised 440
    /// to 972. The changelog window, which had fitted comfortably, then stood
    /// 1060 pixels tall on a screen offering 1032, stuck to the top edge with
    /// its lower end cut off. Limiting on a small screen is the job; granting
    /// more room on a large one is not.
    /// </remarks>
    [AvaloniaFact]
    public void TheScrollLimitIsNeverRaised()
    {
        var window = new ReleaseNotesWindow();
        window.Render(ChangelogResource.All());
        window.Show();

        var scroller = window.FindControl<ScrollViewer>("ContentScroller")!;

        Assert.True(scroller.MaxHeight <= 440d,
            $"The limit stands at {scroller.MaxHeight:0}, the XAML asks for 440.");

        window.Hide();
    }

    /// <summary>The window stays inside the working area, complete changelog and all.</summary>
    [AvaloniaFact]
    public void TheWindowStaysInsideTheWorkingArea()
    {
        var window = new ReleaseNotesWindow();
        window.Render(ChangelogResource.All());
        window.Show();

        var screen = window.Screens.ScreenFromWindow(window)!;
        var frameHeight = (int)Math.Ceiling((window.FrameSize ?? window.Bounds.Size).Height * screen.Scaling);
        var bottom = window.Position.Y + frameHeight;

        Assert.True(bottom <= screen.WorkingArea.Bottom,
            $"The window ends at {bottom}, the working area at {screen.WorkingArea.Bottom}.");

        window.Hide();
    }

    private static ReleaseNotes Release(int major, int minor, int build) => new()
    {
        Version = new Version(major, minor, build),
        Date = new DateOnly(2026, 8, 19),
        Sections =
        [
            new ReleaseNoteSection
            {
                Title = "Fixed",
                Entries = [new ReleaseNoteEntry("Something was put right.")]
            }
        ]
    };
}
