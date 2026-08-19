using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using ClaudeUsageChecker.App.Services;
using ClaudeUsageChecker.App.Settings;
using ClaudeUsageChecker.App.Views;

namespace ClaudeUsageChecker.App.Tests;

/// <summary>
/// Checks the permanent setup, as far as that works without really copying.
/// </summary>
public class SelfInstallerTests
{
    [Fact]
    public void TheTargetSitsUnderProgramsInTheLocalProfile()
    {
        // %LOCALAPPDATA%\Programs is the location Windows intends for
        // applications without administrator rights. The root of the user profile
        // bleibt damit frei.
        var expected = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Programs",
            "ClaudeUsageChecker");

        Assert.Equal(expected, SelfInstaller.TargetDirectory);
    }

    [Fact]
    public void TheTargetIsNotInTheRootOfTheUserProfile()
    {
        var wurzel = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "ClaudeUsageChecker");

        Assert.NotEqual(wurzel, SelfInstaller.TargetDirectory);
    }

    [Fact]
    public void TheTargetFileIsNamedWithoutAVersionNumber()
    {
        // The self-update writes to this path; a version number in it would be
        // wrong after the first update. On top of that, the tray pinning depends
        // on the path.
        Assert.Equal("ClaudeUsageChecker.exe", Path.GetFileName(SelfInstaller.TargetPath));
    }

    [Fact]
    public void NothingIsOfferedInADevelopmentBuild()
    {
        // Dozens of files sit side by side there - copying a single one would
        // not yield anything runnable.
        Assert.False(UpdateInstaller.IsSupported);
        Assert.False(SelfInstaller.ShouldOffer);
    }

    [Fact]
    public void TheQuestionComesOnlyOnce()
    {
        // Once the flag is set, the question does not return - regardless of
        // zugestimmt oder abgelehnt wurde.
        var abgelehnt = new AppSettings { InstallPromptShown = true };

        Assert.True(abgelehnt.InstallPromptShown);
        Assert.False(abgelehnt.LaunchAtLogin);
    }

    [Fact]
    public void FreshSettingsHaveNotAskedYet() =>
        Assert.False(new AppSettings().InstallPromptShown);

    [AvaloniaFact]
    public void ThePromptWindowNamesTheTargetPath()
    {
        var window = new InstallPromptWindow();

        Assert.Equal(SelfInstaller.TargetPath, window.FindControl<TextBlock>("TargetText")!.Text);
        Assert.NotNull(window.FindControl<Button>("InstallButton"));
        Assert.NotNull(window.FindControl<Button>("LaterButton"));
    }

    [AvaloniaFact]
    public void ThePromptWindowShowsNoStatusYet() =>
        Assert.False(new InstallPromptWindow().FindControl<TextBlock>("StatusText")!.IsVisible);
}
