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
    /// <summary>A bundle on a mounted image knows which image that is.</summary>
    /// <remarks>
    /// The volume is what gets ejected afterwards, and it has to be the volume
    /// rather than the bundle: detaching asks for the mount point.
    /// </remarks>
    [Fact]
    public void ABundleOnAnImageNamesTheImageItCameOn()
    {
        Assert.Equal(
            "/Volumes/Claude UsageChecker",
            SelfInstaller.VolumeOf("/Volumes/Claude UsageChecker/ClaudeUsageChecker.app"));

        // A volume with nothing after it is still a volume.
        Assert.Equal("/Volumes/Whatever", SelfInstaller.VolumeOf("/Volumes/Whatever"));
    }

    /// <summary>Anywhere else there is nothing to eject, and that is not a fault.</summary>
    [Fact]
    public void APathOutsideAnImageHasNoVolumeToEject()
    {
        Assert.Null(SelfInstaller.VolumeOf("/Applications/ClaudeUsageChecker.app"));
        Assert.Null(SelfInstaller.VolumeOf("/Users/tester/Downloads/ClaudeUsageChecker.app"));
        Assert.Null(SelfInstaller.VolumeOf("/Volumes/"));
    }

    /// <summary>
    /// Started from an image, the copy is told to eject it once this process has
    /// gone.
    /// </summary>
    /// <remarks>
    /// Three things have to travel together, and each is useless without the
    /// others: a new instance (<c>-n</c>, or open would merely activate the copy
    /// that is quitting), the process to wait for (or the single-instance lock
    /// turns the new one away), and the volume to release (which this process
    /// cannot do while it is still standing on it).
    /// </remarks>
    [Fact]
    public void TheCopyIsToldToEjectTheImageItWasInstalledFrom()
    {
        var arguments = SelfInstaller.StartArguments(
            "/Volumes/Claude UsageChecker/ClaudeUsageChecker.app");

        Assert.Equal("-n", arguments[0]);
        Assert.Contains(UpdateInstaller.WaitArgument, arguments);

        var eject = Array.IndexOf(arguments, UpdateInstaller.EjectArgument);
        Assert.NotEqual(-1, eject);
        Assert.Equal("/Volumes/Claude UsageChecker", arguments[eject + 1]);
    }

    /// <summary>Installed from a folder, nothing is said about ejecting.</summary>
    [Fact]
    public void NothingIsEjectedWhenTheSetupDidNotComeOffAnImage()
    {
        var arguments = SelfInstaller.StartArguments(
            "/Users/tester/Downloads/ClaudeUsageChecker.app");

        Assert.DoesNotContain(UpdateInstaller.EjectArgument, arguments);
        Assert.Equal("-n", arguments[0]);
    }

    /// <summary>The new instance finds the volume on its command line.</summary>
    [Fact]
    public void TheVolumeToEjectIsReadFromTheCommandLine()
    {
        string[] args =
        [
            UpdateInstaller.WaitArgument, "4711",
            UpdateInstaller.EjectArgument, "/Volumes/Claude UsageChecker",
        ];

        Assert.Equal("/Volumes/Claude UsageChecker", StartupArguments.TryReadSourceVolume(args));
        Assert.Null(StartupArguments.TryReadSourceVolume([UpdateInstaller.WaitArgument, "4711"]));

        // A switch with nothing behind it names no volume.
        Assert.Null(StartupArguments.TryReadSourceVolume([UpdateInstaller.EjectArgument]));
    }

    /// <summary>
    /// The word the two versions greet each other with may not be translated.
    /// </summary>
    /// <remarks>
    /// German, alone among the identifiers in this repository, and it stays:
    /// the value is a promise between a version that passes it and its successor
    /// that reads it. Change it, and the successor stops waiting, finds the
    /// single-instance lock held, and ends itself - leaving the machine with
    /// nothing running after an update. The same empty desktop that <c>open</c>
    /// without <c>-n</c> produced on macOS, by a different route.
    /// </remarks>
    [Fact]
    public void TheHandshakeBetweenTwoVersionsKeepsItsWord()
    {
        Assert.Equal("--nach-update", UpdateInstaller.WaitArgument);
    }

    [Fact]
    public void TheTargetSitsUnderProgramsInTheLocalProfile()
    {
        // %LOCALAPPDATA%\Programs is the location Windows intends for
        // applications without administrator rights. That keeps the root of the
        // user profile clear. On macOS the same question was answered decades
        // earlier, and the answer is the applications folder - so this asks only
        // where it applies. Written unguarded, it passed here and failed in CI
        // on the Mac.
        if (OperatingSystem.IsMacOS())
        {
            Assert.Equal("/Applications", SelfInstaller.TargetDirectory);
            return;
        }

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
        // on the path. The rule holds on both platforms; only the extension
        // differs, because on macOS what is put there is a bundle.
        Assert.Equal(
            OperatingSystem.IsMacOS() ? "ClaudeUsageChecker.app" : "ClaudeUsageChecker.exe",
            Path.GetFileName(SelfInstaller.TargetPath));

        Assert.DoesNotContain(
            "0.",
            Path.GetFileName(SelfInstaller.TargetPath),
            StringComparison.Ordinal);
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
