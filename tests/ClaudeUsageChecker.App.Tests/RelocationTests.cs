using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using ClaudeUsageChecker.App.Services;
using ClaudeUsageChecker.App.Settings;
using ClaudeUsageChecker.App.Views;

namespace ClaudeUsageChecker.App.Tests;

/// <summary>
/// Checks that autostart entails moving the application - and that unticking
/// does not.
/// </summary>
/// <remarks>
/// An autostart entry pointing into the downloads folder breaks the first time
/// that folder is cleaned out. Whoever ticks the box expects the application to
/// danach zuverlaessig startet.
/// </remarks>
public class RelocationTests
{
    [AvaloniaFact]
    public void WithoutTheTickNoNoticeAboutMoving()
    {
        using var file = new TemporaryFile();
        var window = Erzeuge(file, new AppSettings { LaunchAtLogin = false }, out _);

        Assert.False(window.FindControl<TextBlock>("RelocationHint")!.IsVisible);
    }

    [AvaloniaFact]
    public void UntickingDoesNotEntailMoving()
    {
        using var file = new TemporaryFile();
        var window = Erzeuge(file, new AppSettings { LaunchAtLogin = true }, out var aufrufe);

        window.FindControl<CheckBox>("LaunchAtLoginBox")!.IsChecked = false;
        Klicke(window, "SaveButton");

        // An application once installed stays where it is - only the autostart
        // entry is removed.
        Assert.Equal(0, aufrufe.Anzahl);
    }

    [AvaloniaFact]
    public void TheNoticeNamesTheTargetPath()
    {
        using var file = new TemporaryFile();
        var window = Erzeuge(file, new AppSettings { LaunchAtLogin = false }, out _);

        window.FindControl<CheckBox>("LaunchAtLoginBox")!.IsChecked = true;

        var hinweis = window.FindControl<TextBlock>("RelocationHint")!;

        // In a development build no move is possible, so no notice either. Where
        // it does appear, it has to name the target path - a surprise would be
        // the worst outcome here.
        if (hinweis.IsVisible)
        {
            Assert.Contains(SelfInstaller.TargetPath, hinweis.Text!, StringComparison.Ordinal);
        }
        else
        {
            Assert.False(SelfInstaller.ShouldOffer);
        }
    }

    private static void Klicke(Window window, string name)
    {
        var knopf = window.FindControl<Button>(name)!;
        knopf.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
    }

    private static SettingsWindow Erzeuge(TemporaryFile file, AppSettings settings, out Zaehler aufrufe)
    {
        var zaehler = new Zaehler();
        aufrufe = zaehler;

        return new SettingsWindow(
            new SettingsStore(file.Path),
            settings,
            oauthTokenStore: null,
            relocate: () =>
            {
                zaehler.Anzahl++;
                return new InstallResult(true, "erledigt");
            });
    }

    private sealed class Zaehler
    {
        public int Anzahl { get; set; }
    }

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
