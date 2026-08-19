using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using ClaudeUsageChecker.App.Views;

namespace ClaudeUsageChecker.App.Tests;

/// <summary>
/// Checks the about window: structure, content, and that nothing extends past
/// the frame.
/// </summary>
public class AboutWindowTests
{
    private static readonly Uri Projektseite = new("https://github.com/sven-reichelt/Claude-UsageChecker");

    [AvaloniaFact]
    public void CanBeCreated()
    {
        var window = new AboutWindow(Projektseite, new Version(0, 6, 0, 0));

        Assert.NotNull(window.FindControl<Image>("LogoImage"));
        Assert.NotNull(window.FindControl<Button>("RepositoryButton"));
        Assert.NotNull(window.FindControl<Button>("CloseButton"));
    }

    [AvaloniaFact]
    public void ShowsTheVersionWithThreeParts()
    {
        var window = new AboutWindow(Projektseite, new Version(0, 6, 0, 0));

        Assert.Equal("Version 0.6.0", window.FindControl<TextBlock>("VersionText")!.Text);
    }

    [AvaloniaFact]
    public void ShowsTheApplicationIcon()
    {
        // A missing image shows up nowhere else: the window opens, only the area
        // at the top would stay blank.
        var window = new AboutWindow(Projektseite, new Version(0, 6, 0));

        Assert.NotNull(window.FindControl<Image>("LogoImage")!.Source);
    }

    [AvaloniaFact]
    public void NamesTheProjectPage()
    {
        var window = new AboutWindow(Projektseite, new Version(0, 6, 0));

        Assert.Contains("github.com/sven-reichelt/Claude-UsageChecker",
            window.FindControl<TextBlock>("RepositoryText")!.Text!, StringComparison.Ordinal);
    }

    /// <summary>
    /// The window does not open the address itself but reports the wish -
    /// starting foreign programs stays gathered in one place.
    /// </summary>
    [AvaloniaFact]
    public void ReportsTheWishForTheProjectPageInsteadOfOpeningItItself()
    {
        var window = new AboutWindow(Projektseite, new Version(0, 6, 0));
        Uri? reported = null;
        window.RepositoryRequested += (_, adresse) => reported = adresse;

        Click(window, "RepositoryButton");

        Assert.Equal(Projektseite, reported);
    }

    [AvaloniaFact]
    public void ReportsTheWishForTheChangelog()
    {
        var window = new AboutWindow(Projektseite, new Version(0, 6, 0));
        var reported = false;
        window.ReleaseNotesRequested += (_, _) => reported = true;

        Click(window, "ReleaseNotesButton");

        Assert.True(reported);
    }

    [AvaloniaFact]
    public void TheContentFitsTheWindow()
    {
        var window = new AboutWindow(Projektseite, new Version(0, 6, 0));

        Assert.True(LayoutProbe.FitsTheWidth(window, out var width),
            $"The content needs {width:0} pixels, the window is {window.Width:0} wide.");
    }

    private static void Click(Window window, string name)
    {
        window.Show();
        var schaltflaeche = window.FindControl<Button>(name)!;
        schaltflaeche.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        window.Hide();
    }
}
