using System.Text.Json;
using ClaudeUsageChecker.App.Services;

namespace ClaudeUsageChecker.App.Tests;

/// <summary>
/// Prueft die Absicherungen des Selbstaustauschs.
/// </summary>
/// <remarks>
/// Die Anwendung laedt hier Fremdcode herunter und fuehrt ihn aus. Die einzige
/// Absicherung dagegen ist die veroeffentlichte Pruefsumme - entsprechend genau
/// muss ihre Auswertung sein. Eine zu grosszuegige Erkennung waere schlimmer
/// als gar keine, weil sie Sicherheit vortaeuscht.
/// </remarks>
public class UpdateInstallerTests
{
    private const string GueltigeSumme = "d07e71e78e774176e768f4c5308d90c66f4e0aafcc495189a4b1115a0e896857";

    [Fact]
    public void PruefsummeWirdAusDerUeblichenSchreibweiseGelesen()
    {
        var inhalt = $"{GueltigeSumme}  ClaudeUsageChecker-0.2.0-win-x64.exe";

        Assert.Equal(GueltigeSumme, UpdateInstaller.LiesPruefsumme(inhalt));
    }

    [Fact]
    public void PruefsummeVertraegtUmbruecheUndLeerraum() =>
        Assert.Equal(GueltigeSumme, UpdateInstaller.LiesPruefsumme($"\n  {GueltigeSumme}   datei.exe \n"));

    [Fact]
    public void PruefsummeInGrossschreibungWirdVereinheitlicht() =>
        Assert.Equal(GueltigeSumme, UpdateInstaller.LiesPruefsumme(GueltigeSumme.ToUpperInvariant() + "  x.exe"));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("keine summe hier")]
    // Zu kurz - eine SHA-256-Summe hat genau 64 Stellen.
    [InlineData("d07e71e7")]
    // Zu lang.
    [InlineData("d07e71e78e774176e768f4c5308d90c66f4e0aafcc495189a4b1115a0e8968571")]
    // Richtige Laenge, aber keine Hexadezimalziffern.
    [InlineData("zzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzz")]
    public void UnbrauchbarePruefsummenWerdenAbgelehnt(string inhalt) =>
        Assert.Null(UpdateInstaller.LiesPruefsumme(inhalt));

    [Fact]
    public void OhneDateiUndPruefsummeWirdNichtsAngeboten()
    {
        var ohneAlles = new UpdateCheckResult { Status = UpdateCheckStatus.UpdateAvailable };
        var nurDatei = new UpdateCheckResult
        {
            Status = UpdateCheckStatus.UpdateAvailable,
            DownloadUrl = new Uri("https://example.invalid/app.exe")
        };
        var nurSumme = new UpdateCheckResult
        {
            Status = UpdateCheckStatus.UpdateAvailable,
            ChecksumUrl = new Uri("https://example.invalid/app.exe.sha256")
        };

        Assert.False(ohneAlles.CanInstall);
        Assert.False(nurDatei.CanInstall);
        Assert.False(nurSumme.CanInstall);
    }

    [Fact]
    public void MitDateiUndPruefsummeWirdAngeboten()
    {
        var vollstaendig = new UpdateCheckResult
        {
            Status = UpdateCheckStatus.UpdateAvailable,
            DownloadUrl = new Uri("https://example.invalid/app.exe"),
            ChecksumUrl = new Uri("https://example.invalid/app.exe.sha256")
        };

        Assert.True(vollstaendig.CanInstall);
    }

    [Fact]
    public void OhneVerfuegbareAktualisierungWirdNichtsAngeboten()
    {
        var aktuell = UpdateCheckResult.UpToDate(new Version(1, 0, 0)) with
        {
            DownloadUrl = new Uri("https://example.invalid/app.exe"),
            ChecksumUrl = new Uri("https://example.invalid/app.exe.sha256")
        };

        Assert.False(aktuell.CanInstall);
    }

    [Fact]
    public void DateienWerdenAusDerAntwortVonGitHubGelesen()
    {
        using var doc = JsonDocument.Parse("""
            {
              "assets": [
                { "name": "ClaudeUsageChecker-0.3.0-win-x64.exe",
                  "browser_download_url": "https://github.com/x/y/releases/download/v0.3.0/app.exe" },
                { "name": "ClaudeUsageChecker-0.3.0-win-x64.exe.sha256",
                  "browser_download_url": "https://github.com/x/y/releases/download/v0.3.0/app.exe.sha256" }
              ]
            }
            """);

        Assert.Equal(
            new Uri("https://github.com/x/y/releases/download/v0.3.0/app.exe"),
            GitHubReleaseUpdateService.FindAsset(doc.RootElement, ".exe"));
        Assert.Equal(
            new Uri("https://github.com/x/y/releases/download/v0.3.0/app.exe.sha256"),
            GitHubReleaseUpdateService.FindAsset(doc.RootElement, ".exe.sha256"));
    }

    [Fact]
    public void EineAdresseOhneHttpsWirdVerworfen()
    {
        // Sonst liesse sich der Download auf eine ungesicherte Verbindung lenken.
        using var doc = JsonDocument.Parse("""
            {"assets":[{"name":"app.exe","browser_download_url":"http://example.invalid/app.exe"}]}
            """);

        Assert.Null(GitHubReleaseUpdateService.FindAsset(doc.RootElement, ".exe"));
    }

    [Fact]
    public void OhneAngehaengteDateienWirdNichtsGefunden()
    {
        using var doc = JsonDocument.Parse("""{"tag_name":"v0.3.0"}""");

        Assert.Null(GitHubReleaseUpdateService.FindAsset(doc.RootElement, ".exe"));
    }

    [Theory]
    [InlineData(1234, "--nach-update", "1234")]
    [InlineData(42, "irgendwas", "--nach-update", "42")]
    public void DieKennungDerVorgaengerinstanzWirdGelesen(int erwartet, params string[] args) =>
        Assert.Equal(erwartet, StartupArguments.TryReadPredecessorId(args));

    [Theory]
    [InlineData]
    [InlineData("--nach-update")]
    [InlineData("--nach-update", "keinezahl")]
    [InlineData("--nach-update", "0")]
    [InlineData("--nach-update", "-5")]
    [InlineData("--anderer-schalter", "1234")]
    public void UnbrauchbareAngabenWerdenIgnoriert(params string[] args) =>
        Assert.Null(StartupArguments.TryReadPredecessorId(args));
}
