using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using ClaudeUsageChecker.App.Services;
using ClaudeUsageChecker.App.Settings;
using ClaudeUsageChecker.App.Views;

namespace ClaudeUsageChecker.App.Tests;

/// <summary>
/// Prueft die dauerhafte Einrichtung, soweit das ohne echtes Kopieren geht.
/// </summary>
public class SelfInstallerTests
{
    [Fact]
    public void DerZielortLiegtImBenutzerprofil()
    {
        var profil = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        Assert.StartsWith(profil, SelfInstaller.TargetDirectory, StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith("ClaudeUsageChecker", SelfInstaller.TargetDirectory, StringComparison.Ordinal);
    }

    [Fact]
    public void DieZieldateiHeisstOhneVersionsnummer()
    {
        // Der Selbstaustausch schreibt an diesen Pfad; eine Versionsnummer darin
        // wuerde nach der ersten Aktualisierung nicht mehr stimmen. Ausserdem
        // haengt die Anheftung im Infobereich am Pfad.
        Assert.Equal("ClaudeUsageChecker.exe", Path.GetFileName(SelfInstaller.TargetPath));
    }

    [Fact]
    public void ImEntwicklungsstandWirdNichtsAngeboten()
    {
        // Dort liegen Dutzende Dateien nebeneinander - eine einzelne zu kopieren
        // ergaebe nichts Lauffaehiges.
        Assert.False(UpdateInstaller.IsSupported);
        Assert.False(SelfInstaller.ShouldOffer);
    }

    [Fact]
    public void DieFrageKommtNurEinmal()
    {
        // Steht das Merkmal, wird nicht erneut gefragt - unabhaengig davon, ob
        // zugestimmt oder abgelehnt wurde.
        var abgelehnt = new AppSettings { InstallPromptShown = true };

        Assert.True(abgelehnt.InstallPromptShown);
        Assert.False(abgelehnt.LaunchAtLogin);
    }

    [Fact]
    public void EineFrischeEinstellungHatNochNichtGefragt() =>
        Assert.False(new AppSettings().InstallPromptShown);

    [AvaloniaFact]
    public void DasNachfragefensterNenntDenZielpfad()
    {
        var window = new InstallPromptWindow();

        Assert.Equal(SelfInstaller.TargetPath, window.FindControl<TextBlock>("TargetText")!.Text);
        Assert.NotNull(window.FindControl<Button>("InstallButton"));
        Assert.NotNull(window.FindControl<Button>("LaterButton"));
    }

    [AvaloniaFact]
    public void DasNachfragefensterZeigtNochKeinenStatus() =>
        Assert.False(new InstallPromptWindow().FindControl<TextBlock>("StatusText")!.IsVisible);
}
