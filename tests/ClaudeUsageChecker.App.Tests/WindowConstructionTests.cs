using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using ClaudeUsageChecker.App.Settings;
using ClaudeUsageChecker.App.Views;
using ClaudeUsageChecker.Core.Models;
using ClaudeUsageChecker.Core.Services;

namespace ClaudeUsageChecker.App.Tests;

/// <summary>
/// Makes sure the windows can be created at all and that their controls named
/// with x:Name are wired up.
/// </summary>
/// <remarks>
/// Background: a hand-written, parameterless InitializeComponent method hides
/// the one Avalonia generates. The XAML is still loaded, but the fields of the
/// named controls stay null - and the constructor fails with a
/// NullReferenceException. That compiles without error and only shows when the
/// window is opened. These tests catch exactly that.
/// </remarks>
public class WindowConstructionTests
{
    [AvaloniaFact]
    public void DetailsWindow_CanBeCreated()
    {
        var window = new DetailsWindow();

        Assert.NotNull(window.FindControl<Button>("RefreshButton"));
        Assert.NotNull(window.FindControl<StackPanel>("WindowsPanel"));
        Assert.NotNull(window.FindControl<TextBlock>("FooterText"));
    }

    [AvaloniaFact]
    public void DetailsWindow_RendersUsageData()
    {
        var window = new DetailsWindow();

        window.Render(ReadyState());

        var panel = window.FindControl<StackPanel>("WindowsPanel")!;
        Assert.Equal(2, panel.Children.Count);
    }

    [AvaloniaFact]
    public void DetailsWindow_ShowsANoticeWithoutAToken()
    {
        var window = new DetailsWindow();

        window.Render(new UsageState { Kind = UsageStateKind.NotConfigured });

        Assert.True(window.FindControl<Border>("MessageBorder")!.IsVisible);
    }

    [AvaloniaFact]
    public void SettingsWindow_CanBeCreated()
    {
        using var settingsFile = new TemporaryFile();

        var window = new SettingsWindow(
            new SettingsStore(settingsFile.Path),
            new AppSettings());

        Assert.NotNull(window.FindControl<NumericUpDown>("IntervalBox"));
        Assert.NotNull(window.FindControl<Button>("SaveButton"));
    }

    [AvaloniaFact]
    public void SettingsWindow_AdoptsExistingSettings()
    {
        using var settingsFile = new TemporaryFile();
        var settings = new AppSettings { PollIntervalSeconds = 600, CheckForUpdates = false };

        var window = new SettingsWindow(new SettingsStore(settingsFile.Path), settings);

        Assert.Equal(600m, window.FindControl<NumericUpDown>("IntervalBox")!.Value);
        Assert.False(window.FindControl<CheckBox>("CheckUpdatesBox")!.IsChecked);
    }

    private static UsageState ReadyState() => new()
    {
        Kind = UsageStateKind.Ready,
        Snapshot = new UsageSnapshot
        {
            Session = new UsageWindow(33, DateTimeOffset.UtcNow.AddHours(2)),
            Weekly = new UsageWindow(13, DateTimeOffset.UtcNow.AddDays(4)),
            RetrievedAt = DateTimeOffset.UtcNow
        }
    };

    /// <summary>Temporary path, so that tests leave the user's settings alone.</summary>
    private sealed class TemporaryFile : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            $"cuc-test-{Guid.NewGuid():N}.json");

        public void Dispose()
        {
            if (File.Exists(Path))
            {
                File.Delete(Path);
            }
        }
    }
}
