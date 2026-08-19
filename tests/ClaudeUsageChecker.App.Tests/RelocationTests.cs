using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using ClaudeUsageChecker.App.Services;
using ClaudeUsageChecker.App.Settings;
using ClaudeUsageChecker.App.Views;

namespace ClaudeUsageChecker.App.Tests;

/// <summary>
/// Prueft, dass der Autostart das Umziehen nach sich zieht - und das Abwaehlen
/// eben nicht.
/// </summary>
/// <remarks>
/// Ein Autostart-Eintrag, der in den Download-Ordner zeigt, bricht beim ersten
/// Aufraeumen dort. Wer den Haken setzt, erwartet aber, dass die Anwendung
/// danach zuverlaessig startet.
/// </remarks>
public class RelocationTests
{
    [AvaloniaFact]
    public void OhneHakenKeinHinweisAufsUmziehen()
    {
        using var datei = new TemporaereDatei();
        var window = Erzeuge(datei, new AppSettings { LaunchAtLogin = false }, out _);

        Assert.False(window.FindControl<TextBlock>("RelocationHint")!.IsVisible);
    }

    [AvaloniaFact]
    public void DasAbwaehlenZiehtKeinUmziehenNachSich()
    {
        using var datei = new TemporaereDatei();
        var window = Erzeuge(datei, new AppSettings { LaunchAtLogin = true }, out var aufrufe);

        window.FindControl<CheckBox>("LaunchAtLoginBox")!.IsChecked = false;
        Klicke(window, "SaveButton");

        // Die einmal eingerichtete Anwendung bleibt, wo sie ist - entfernt wird
        // nur der Autostart-Eintrag.
        Assert.Equal(0, aufrufe.Anzahl);
    }

    [AvaloniaFact]
    public void DerHinweisNenntDenZielpfad()
    {
        using var datei = new TemporaereDatei();
        var window = Erzeuge(datei, new AppSettings { LaunchAtLogin = false }, out _);

        window.FindControl<CheckBox>("LaunchAtLoginBox")!.IsChecked = true;

        var hinweis = window.FindControl<TextBlock>("RelocationHint")!;

        // Im Entwicklungsstand ist kein Umzug moeglich, also auch kein Hinweis.
        // Steht er, muss er den Zielpfad nennen - eine Ueberraschung waere hier
        // das Schlechteste.
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

    private static SettingsWindow Erzeuge(TemporaereDatei datei, AppSettings settings, out Zaehler aufrufe)
    {
        var zaehler = new Zaehler();
        aufrufe = zaehler;

        return new SettingsWindow(
            new FakeSecretStore(),
            new SettingsStore(datei.Path),
            settings,
            validateToken: null,
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

    private sealed class TemporaereDatei : IDisposable
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
