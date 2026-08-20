using System.Text.Json;
using ClaudeUsageChecker.App.Services;

namespace ClaudeUsageChecker.App.Tests;

/// <summary>
/// Checks the safeguards of the self-update.
/// </summary>
/// <remarks>
/// The application downloads foreign code here and executes it. The only
/// safeguard against that is the published checksum - so its evaluation has to
/// be correspondingly strict. A recognition that is too generous would be worse
/// than none at all, because it feigns safety.
/// </remarks>
public class UpdateInstallerTests
{
    private const string ValidChecksum = "d07e71e78e774176e768f4c5308d90c66f4e0aafcc495189a4b1115a0e896857";

    [Fact]
    public void TheChecksumIsReadFromTheUsualNotation()
    {
        var content = $"{ValidChecksum}  ClaudeUsageChecker-0.2.0-win-x64.exe";

        Assert.Equal(ValidChecksum, UpdateInstaller.ReadChecksum(content));
    }

    [Fact]
    public void TheChecksumToleratesLineBreaksAndWhitespace() =>
        Assert.Equal(ValidChecksum, UpdateInstaller.ReadChecksum($"\n  {ValidChecksum}   file.exe \n"));

    [Fact]
    public void AnUppercaseChecksumIsNormalised() =>
        Assert.Equal(ValidChecksum, UpdateInstaller.ReadChecksum(ValidChecksum.ToUpperInvariant() + "  x.exe"));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("keine summe hier")]
    // Too short - a SHA-256 sum has exactly 64 digits.
    [InlineData("d07e71e7")]
    // Too long.
    [InlineData("d07e71e78e774176e768f4c5308d90c66f4e0aafcc495189a4b1115a0e8968571")]
    // Right length, but not hexadecimal digits.
    [InlineData("zzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzz")]
    public void UnusableChecksumsAreRejected(string content) =>
        Assert.Null(UpdateInstaller.ReadChecksum(content));

    [Fact]
    public void WithoutFileAndChecksumNothingIsOffered()
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
    public void WithoutAnAvailableUpdateNothingIsOffered()
    {
        var current = UpdateCheckResult.UpToDate(new ProgramVersion(new Version(1, 0, 0))) with
        {
            DownloadUrl = new Uri("https://example.invalid/app.exe"),
            ChecksumUrl = new Uri("https://example.invalid/app.exe.sha256")
        };

        Assert.False(current.CanInstall);
    }

    [Fact]
    public void FilesAreReadFromGitHubsResponse()
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
    public void AnAddressWithoutHttpsIsDiscarded()
    {
        // Otherwise the download could be steered onto an unsecured connection.
        using var doc = JsonDocument.Parse("""
            {"assets":[{"name":"app.exe","browser_download_url":"http://example.invalid/app.exe"}]}
            """);

        Assert.Null(GitHubReleaseUpdateService.FindAsset(doc.RootElement, ".exe"));
    }

    [Fact]
    public void WithoutAttachedFilesNothingIsFound()
    {
        using var doc = JsonDocument.Parse("""{"tag_name":"v0.3.0"}""");

        Assert.Null(GitHubReleaseUpdateService.FindAsset(doc.RootElement, ".exe"));
    }

    [Theory]
    [InlineData(1234, "--nach-update", "1234")]
    [InlineData(42, "irgendwas", "--nach-update", "42")]
    public void DieKennungDerVorgaengerinstanzWirdGelesen(int expected, params string[] args) =>
        Assert.Equal(expected, StartupArguments.TryReadPredecessorId(args));

    [Theory]
    [InlineData]
    [InlineData("--nach-update")]
    [InlineData("--nach-update", "keinezahl")]
    [InlineData("--nach-update", "0")]
    [InlineData("--nach-update", "-5")]
    [InlineData("--anderer-schalter", "1234")]
    public void UnusableValuesAreIgnored(params string[] args) =>
        Assert.Null(StartupArguments.TryReadPredecessorId(args));
}
