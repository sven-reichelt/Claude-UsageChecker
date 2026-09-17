using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using ClaudeUsageChecker.App.Services;
using ClaudeUsageChecker.App.Settings;
using ClaudeUsageChecker.App.Views;

namespace ClaudeUsageChecker.App.Tests;

/// <summary>
/// The question the check in the background asks, and the switch that decides
/// what happens at startup.
/// </summary>
public class UpdateAvailableWindowTests
{
    [AvaloniaFact]
    public void UpdateNowAsksForTheInstallAndIsNoReminder()
    {
        var window = Build(out var installs, out var reminders);
        window.Show();

        Click(window, "InstallButton");
        window.Close();

        Assert.Equal(1, installs.Count);
        Assert.Equal(0, reminders.Count);
    }

    [AvaloniaFact]
    public void TomorrowAsksForAReminder()
    {
        var window = Build(out var installs, out var reminders);
        window.Show();

        Click(window, "LaterButton");

        Assert.Equal(0, installs.Count);
        Assert.Equal(1, reminders.Count);
        Assert.False(window.IsVisible);
    }

    /// <summary>
    /// Closed without a decision counts as tomorrow: asking again in two hours
    /// would be nagging, never again would be forgetting.
    /// </summary>
    [AvaloniaFact]
    public void ClosingWithoutADecisionCountsAsTomorrow()
    {
        var window = Build(out _, out var reminders);
        window.Show();

        window.Close();

        Assert.Equal(1, reminders.Count);
    }

    /// <summary>A failed attempt is not a decision about the version.</summary>
    [AvaloniaFact]
    public void AFailedInstallGivesTheButtonsBackAndStillCountsAsUndecided()
    {
        var window = Build(out _, out var reminders);
        window.Show();

        Click(window, "InstallButton");
        window.SetProgress("download failed", busy: false);
        window.Close();

        Assert.True(window.FindControl<Button>("InstallButton")!.IsEnabled);
        Assert.Equal(1, reminders.Count);
    }

    [AvaloniaFact]
    public void WhileInstallingNeitherButtonCanBePressed()
    {
        var window = Build(out _, out _);

        window.SetProgress("downloading", busy: true);

        Assert.False(window.FindControl<Button>("InstallButton")!.IsEnabled);
        Assert.False(window.FindControl<Button>("LaterButton")!.IsEnabled);
        Assert.True(window.FindControl<TextBlock>("StatusText")!.IsVisible);
    }

    /// <summary>A copy that cannot replace itself offers the release page instead.</summary>
    [AvaloniaFact]
    public void ACopyThatCannotReplaceItselfLeadsToTheReleasePage()
    {
        var window = new UpdateAvailableWindow(Update(), canInstall: false);

        Assert.False(window.FindControl<Button>("InstallButton")!.IsVisible);
        Assert.True(window.FindControl<Button>("ReleasePageButton")!.IsVisible);
    }

    /// <summary>Without a checksum there is nothing to install, whatever the copy could do.</summary>
    [AvaloniaFact]
    public void AReleaseWithoutItsChecksumIsNotOfferedForInstalling()
    {
        var window = new UpdateAvailableWindow(Update() with { ChecksumUrl = null }, canInstall: true);

        Assert.False(window.CanInstall);
        Assert.False(window.FindControl<Button>("InstallButton")!.IsVisible);
    }

    /// <summary>A keystroke meant for another window must not start or postpone an update.</summary>
    [AvaloniaFact]
    public void TheQuestionDoesNotTakeTheKeyboard()
    {
        var window = Build(out _, out _);

        Assert.False(window.ShowActivated);
        Assert.False(window.FindControl<Button>("InstallButton")!.IsDefault);
        Assert.False(window.FindControl<Button>("LaterButton")!.IsDefault);
    }

    [AvaloniaFact]
    public void AutomaticUpdatesAreOnByDefaultAndSaved()
    {
        var path = Path.Combine(Path.GetTempPath(), $"cuc-test-{Guid.NewGuid():N}.json");
        try
        {
            var store = new SettingsStore(path);
            var window = new SettingsWindow(store, new AppSettings(), applyAutostart: _ => { });
            var box = window.FindControl<CheckBox>("AutoUpdateBox")!;

            var byDefault = box.IsChecked;
            box.IsChecked = false;
            window.Show();
            Click(window, "SaveButton");

            Assert.True(byDefault);
            Assert.False(store.Load().AutoUpdate);
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// Automatic updates check at startup regardless, so the switch for that
    /// check is unavailable while they are on.
    /// </summary>
    [AvaloniaFact]
    public void TheStartupCheckIsOnlyAChoiceWithoutAutomaticUpdates()
    {
        var window = new SettingsWindow(
            new SettingsStore(Path.Combine(Path.GetTempPath(), $"cuc-test-{Guid.NewGuid():N}.json")),
            new AppSettings(), applyAutostart: _ => { });
        var automatic = window.FindControl<CheckBox>("AutoUpdateBox")!;
        var startupCheck = window.FindControl<CheckBox>("CheckUpdatesBox")!;

        var whileAutomatic = startupCheck.IsEnabled;
        automatic.IsChecked = false;

        Assert.False(whileAutomatic);
        Assert.True(startupCheck.IsEnabled);
    }

    [Fact]
    public void AFileFromAnEarlierVersionHasAutomaticUpdatesOn()
    {
        var path = Path.Combine(Path.GetTempPath(), $"cuc-test-{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(path, """{ "checkForUpdates": false }""");

            var loaded = new SettingsStore(path).Load();

            Assert.True(loaded.AutoUpdate);
            Assert.False(loaded.CheckForUpdates);
        }
        finally
        {
            File.Delete(path);
        }
    }

    internal static UpdateCheckResult Update() => new()
    {
        Status = UpdateCheckStatus.UpdateAvailable,
        AvailableVersion = new ProgramVersion(new Version(1, 0, 2)),
        ReleasePage = new Uri("https://example.invalid/releases/v1.0.2"),
        DownloadUrl = new Uri("https://example.invalid/ClaudeUsageChecker.exe"),
        ChecksumUrl = new Uri("https://example.invalid/ClaudeUsageChecker.exe.sha256"),
        Message = "Version 1.0.2 is available (installed: 1.0.1)."
    };

    private static UpdateAvailableWindow Build(out Counter installs, out Counter reminders)
    {
        var window = new UpdateAvailableWindow(Update(), canInstall: true);
        var i = new Counter();
        var r = new Counter();
        window.InstallRequested += (_, _) => i.Count++;
        window.RemindLaterRequested += (_, _) => r.Count++;
        installs = i;
        reminders = r;
        return window;
    }

    private static void Click(Window window, string name) =>
        window.FindControl<Button>(name)!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

    internal sealed class Counter
    {
        public int Count { get; set; }
    }
}
