using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using ClaudeUsageChecker.App.Settings;
using ClaudeUsageChecker.App.Views;

namespace ClaudeUsageChecker.App.Tests;

/// <summary>
/// Checks the choice between light, dark and the system.
/// </summary>
/// <remarks>
/// The application followed the system from the start, without anyone ever
/// having looked at the result - the theme was never chosen, only inherited.
/// What these check is the plumbing: that a choice survives the settings file,
/// that anything unreadable lands on the system rather than in a colour scheme
/// nobody picked, and that the picker takes effect at once. Whether it reads
/// well in the dark is a question for the eye, and the rendering tests draw
/// every window that way for it.
/// </remarks>
public class AppearanceTests
{
    [Fact]
    public void FollowingTheSystemIsTheDefault() =>
        Assert.Equal(AppearanceMode.System, new AppSettings().AppearanceMode);

    /// <summary>Anything unreadable counts as the system.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("midnight")]
    public void AnUnknownValueFollowsTheSystem(string? stored) =>
        Assert.Equal(AppearanceMode.System, new AppSettings { Appearance = stored }.AppearanceMode);

    /// <summary>The stored text is readable, not an ordinal.</summary>
    [Theory]
    [InlineData(AppearanceMode.System, "system")]
    [InlineData(AppearanceMode.Light, "light")]
    [InlineData(AppearanceMode.Dark, "dark")]
    public void TheChoiceIsWrittenDownAsText(AppearanceMode mode, string expected)
    {
        var settings = new AppSettings { AppearanceMode = mode };

        Assert.Equal(expected, settings.Appearance);
        Assert.Equal(mode, settings.AppearanceMode);
    }

    /// <summary>
    /// Following the system is Avalonia's default variant, not a third colour.
    /// </summary>
    [Fact]
    public void FollowingTheSystemMeansAskingIt()
    {
        Assert.Equal(ThemeVariant.Default, AppearanceMode.System.ToVariant());
        Assert.Equal(ThemeVariant.Light, AppearanceMode.Light.ToVariant());
        Assert.Equal(ThemeVariant.Dark, AppearanceMode.Dark.ToVariant());
    }

    /// <summary>The choice survives the way through the file.</summary>
    [Fact]
    public void TheChoiceSurvivesTheSettingsFile()
    {
        var path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(), $"cuc-appearance-{Guid.NewGuid():N}.json");

        try
        {
            new SettingsStore(path).Save(new AppSettings { AppearanceMode = AppearanceMode.Dark });

            Assert.Contains("\"appearance\": \"dark\"", File.ReadAllText(path), StringComparison.Ordinal);
            Assert.Equal(AppearanceMode.Dark, new SettingsStore(path).Load().AppearanceMode);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    /// <summary>
    /// Choosing in the picker takes effect at once, before saving.
    /// </summary>
    /// <remarks>
    /// Colour is the one setting whose effect cannot be described in a
    /// sentence - it has to be seen, and seeing it means being able to change
    /// one's mind before saving.
    /// </remarks>
    [AvaloniaFact]
    public void ChoosingAppliesTheThemeStraightAway()
    {
        var application = Avalonia.Application.Current!;
        var before = application.RequestedThemeVariant;

        try
        {
            using var file = new TemporaryFile();
            var window = new SettingsWindow(
                new SettingsStore(file.Path), new AppSettings(), applyAutostart: _ => { });

            window.FindControl<ComboBox>("ThemeBox")!.SelectedIndex = (int)AppearanceMode.Dark;

            Assert.Equal(ThemeVariant.Dark, application.RequestedThemeVariant);
        }
        finally
        {
            application.RequestedThemeVariant = before;
        }
    }

    /// <summary>
    /// Cancelling puts back what was there.
    /// </summary>
    /// <remarks>
    /// Applying while choosing means there is something to undo - unlike every
    /// other setting in that window, which only takes effect on saving.
    /// </remarks>
    [AvaloniaFact]
    public void CancellingRestoresTheAppearanceThatWasThere()
    {
        var application = Avalonia.Application.Current!;
        var before = application.RequestedThemeVariant;

        try
        {
            using var file = new TemporaryFile();
            var window = new SettingsWindow(
                new SettingsStore(file.Path),
                new AppSettings { AppearanceMode = AppearanceMode.Light },
                applyAutostart: _ => { });

            window.Show();
            window.FindControl<ComboBox>("ThemeBox")!.SelectedIndex = (int)AppearanceMode.Dark;
            Assert.Equal(ThemeVariant.Dark, application.RequestedThemeVariant);

            window.FindControl<Button>("CancelButton")!.RaiseEvent(
                new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));

            Assert.Equal(ThemeVariant.Light, application.RequestedThemeVariant);
        }
        finally
        {
            application.RequestedThemeVariant = before;
        }
    }

    private sealed class TemporaryFile : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(), $"cuc-appearance-{Guid.NewGuid():N}.json");

        public void Dispose()
        {
            if (File.Exists(Path))
            {
                File.Delete(Path);
            }
        }
    }
}
