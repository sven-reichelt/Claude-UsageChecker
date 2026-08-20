using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using ClaudeUsageChecker.App.Settings;
using ClaudeUsageChecker.App.Views;

namespace ClaudeUsageChecker.App.Tests;

/// <summary>
/// Checks the way the choice of update channel is revealed.
/// </summary>
/// <remarks>
/// It is hidden behind five clicks on the version number. Not because it is a
/// secret, but because nobody should end up on a half-finished build without
/// having gone looking. What matters for a test is the pair either way: it must
/// not appear too early, and it must not stay unreachable.
/// </remarks>
public class ChannelRevealTests
{
    [AvaloniaFact]
    public void TheChoiceIsHiddenAtFirst()
    {
        using var file = new TemporaryFile();
        var window = Create(file, new AppSettings());

        Assert.False(Section(window).IsVisible);
    }

    [AvaloniaFact]
    public void FourClicksAreNotEnough()
    {
        using var file = new TemporaryFile();
        var window = Create(file, new AppSettings());
        window.Show();

        Click(window, SettingsWindow.ClicksToRevealChannel - 1);

        Assert.False(Section(window).IsVisible);

        window.Hide();
    }

    [AvaloniaFact]
    public void TheFifthClickRevealsIt()
    {
        using var file = new TemporaryFile();
        var window = Create(file, new AppSettings());
        window.Show();

        Click(window, SettingsWindow.ClicksToRevealChannel);

        Assert.True(Section(window).IsVisible);

        window.Hide();
    }

    /// <summary>
    /// Where it has been found before it is there from the start.
    /// </summary>
    [AvaloniaFact]
    public void OnceFoundItStaysVisible()
    {
        using var file = new TemporaryFile();
        var window = Create(file, new AppSettings { UpdateChannelShown = true });

        Assert.True(Section(window).IsVisible);
    }

    /// <summary>
    /// Whoever is on a pre-release sees the choice whether or not they remember
    /// the trick - otherwise there would be no way back.
    /// </summary>
    [AvaloniaFact]
    public void APreReleaseChoiceIsAlwaysVisible()
    {
        using var file = new TemporaryFile();
        var window = Create(file, new AppSettings { Channel = UpdateChannel.PreRelease });

        Assert.True(Section(window).IsVisible);
        Assert.Equal(1, window.FindControl<ComboBox>("ChannelBox")!.SelectedIndex);
    }

    private static void Click(SettingsWindow window, int times)
    {
        var version = window.FindControl<TextBlock>("VersionText")!;

        for (var i = 0; i < times; i++)
        {
            version.RaiseEvent(new PointerPressedEventArgs(
                version, new Pointer(i, PointerType.Mouse, true), version, default,
                0, new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonPressed),
                KeyModifiers.None));
        }
    }

    private static StackPanel Section(SettingsWindow window) =>
        window.FindControl<StackPanel>("ChannelSection")!;

    private static SettingsWindow Create(TemporaryFile file, AppSettings settings) =>
        new(new SettingsStore(file.Path), settings, applyAutostart: _ => { });

    private sealed class TemporaryFile : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(), $"cuc-channel-{Guid.NewGuid():N}.json");

        public void Dispose()
        {
            if (File.Exists(Path))
            {
                File.Delete(Path);
            }
        }
    }
}
