using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

using ClaudeUsageChecker.Core.Localization;

namespace ClaudeUsageChecker.App.Services;

/// <summary>Result of an attempt to install.</summary>
public sealed record InstallResult(bool Succeeded, string Message)
{
    public static InstallResult Failed(string message) => new(false, message);
}

/// <summary>
/// Downloads a new version and replaces the running file.
/// </summary>
/// <remarks>
/// <para>
/// This deliberately downloads and executes foreign code - a departure from the
/// original restraint, made because a mere notice tends to be left lying around.
/// The safeguard therefore rests on the checksum: without a published SHA-256 sum
/// and without a match, nothing is installed. The addresses come from the GitHub
/// response for exactly this repository, not from strings pieced together.
/// </para>
/// <para>
/// Windows does not allow a running file to be overwritten, but it does allow
/// renaming. The procedure rests on exactly that: rename, put the new file in the
/// old place, start the new version, end this one. The new instance clears away
/// the renamed old version on startup.
/// </para>
/// </remarks>
public sealed class UpdateInstaller(HttpClient httpClient)
{
    /// <summary>Extension of the old version that has been set aside.</summary>
    public const string BackupSuffix = ".alt";

    /// <summary>Switch that makes the new version wait for the old one to end.</summary>
    public const string WaitArgument = "--nach-update";

    /// <summary>
    /// Whether the running version can replace itself.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Under Windows that means a release as a single file - in a development
    /// build dozens of files sit side by side, each of which would have to be
    /// swapped. It is recognised from the absence of the like-named library
    /// next to the executable, which answers precisely the question that
    /// matters: is swapping this one file enough? <c>Assembly.Location</c>
    /// could tell as well, but its behaviour in single files is rightly
    /// considered a pitfall.
    /// </para>
    /// <para>
    /// On macOS the application is a bundle, so the question is whether it runs
    /// out of one and whether the folder holding it can be written to. An
    /// application in /Applications belongs to whoever installed it, and
    /// another account may be able to read it and not to replace it.
    /// </para>
    /// </remarks>
    public static bool IsSupported
    {
        get
        {
            if (Environment.ProcessPath is not { Length: > 0 } path)
            {
                return false;
            }

            if (OperatingSystem.IsMacOS())
            {
                return MacOsBundle.CanReplace(path);
            }

            return OperatingSystem.IsWindows() && !File.Exists(Path.ChangeExtension(path, ".dll"));
        }
    }

    /// <summary>
    /// Downloads the new version, verifies it and puts it in place of the old
    /// one. On success the new instance is already running - the caller has to end
    /// itself afterwards.
    /// </summary>
    public async Task<InstallResult> InstallAsync(
        UpdateCheckResult update, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(update);

        if (!IsSupported)
        {
            return InstallResult.Failed(
                T.UpdaterNotSelfReplaceable);
        }

        if (update is not { DownloadUrl: { } downloadUrl, ChecksumUrl: { } checksumUrl })
        {
            return InstallResult.Failed(
                T.UpdaterMissingFileOrChecksum);
        }

        var ownPath = Environment.ProcessPath!;
        var suffix = OperatingSystem.IsMacOS() ? ".zip" : ".exe";
        var temp = Path.Combine(Path.GetTempPath(), $"ClaudeUsageChecker-{Guid.NewGuid():N}{suffix}");

        try
        {
            await DownloadAsync(downloadUrl, temp, cancellationToken).ConfigureAwait(false);

            var expected = await LoadChecksumAsync(checksumUrl, cancellationToken).ConfigureAwait(false);
            if (expected is null)
            {
                return InstallResult.Failed(T.UpdaterChecksumUnreadable);
            }

            var actual = await ComputeChecksumAsync(temp, cancellationToken).ConfigureAwait(false);
            if (!string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase))
            {
                return InstallResult.Failed(
                    T.UpdaterChecksumMismatch);
            }

            return OperatingSystem.IsMacOS()
                ? await ReplaceBundleAsync(ownPath, temp, cancellationToken).ConfigureAwait(false)
                : PutInPlace(ownPath, temp);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return InstallResult.Failed(T.UpdaterDownloadFailed(ex.Message));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return InstallResult.Failed(T.UpdaterSaveFailed(ex.Message));
        }
        finally
        {
            DeleteQuietly(temp);
        }
    }

    /// <summary>
    /// The macOS route: unpack, ask whether macOS would run it, then swap.
    /// </summary>
    /// <remarks>
    /// The signature check is the part the Windows route has no counterpart
    /// for, and it is worth more than the checksum beside it. A checksum proves
    /// that the file is the one the server sent; a signature proves who built
    /// it. Both are cheap, and neither replaces the other: a repository taken
    /// over would serve a matching checksum for whatever it liked.
    /// </remarks>
    private static async Task<InstallResult> ReplaceBundleAsync(
        string ownPath, string archive, CancellationToken cancellationToken)
    {
        if (MacOsBundle.Of(ownPath) is not { } bundle)
        {
            return InstallResult.Failed(T.UpdaterNotSelfReplaceable);
        }

        var workspace = Path.Combine(Path.GetTempPath(), $"ClaudeUsageChecker-{Guid.NewGuid():N}");

        try
        {
            if (await MacOsBundle.ExtractAsync(archive, workspace, cancellationToken)
                    .ConfigureAwait(false) is not { } unpacked)
            {
                return InstallResult.Failed(T.UpdaterSaveFailed(workspace));
            }

            if (!await MacOsBundle.IsAcceptableAsync(unpacked, cancellationToken).ConfigureAwait(false))
            {
                return InstallResult.Failed(T.UpdaterSignatureRejected);
            }

            return await MacOsBundle.ReplaceAsync(bundle, unpacked, cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            try
            {
                if (Directory.Exists(workspace))
                {
                    Directory.Delete(workspace, recursive: true);
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Temporary files that outlive us are the smaller problem.
            }
        }
    }

    private async Task DownloadAsync(Uri url, string target, CancellationToken cancellationToken)
    {
        using var response = await httpClient
            .GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        await using var source = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await using var file = File.Create(target);
        await source.CopyToAsync(file, cancellationToken).ConfigureAwait(false);
    }

    private async Task<string?> LoadChecksumAsync(Uri url, CancellationToken cancellationToken)
    {
        var content = await httpClient.GetStringAsync(url, cancellationToken).ConfigureAwait(false);
        return ReadChecksum(content);
    }

    /// <summary>
    /// Extracts the sum from a file of the form "&lt;hash&gt;  &lt;filename&gt;".
    /// </summary>
    internal static string? ReadChecksum(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        var firstWord = content.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)[0];

        // A SHA-256 sum has 64 digits in hexadecimal.
        return firstWord.Length == 64 && IsHex(firstWord) ? firstWord.ToLowerInvariant() : null;
    }

    private static bool IsHex(string value)
    {
        foreach (var c in value)
        {
            if (!Uri.IsHexDigit(c))
            {
                return false;
            }
        }

        return true;
    }

    private static async Task<string> ComputeChecksumAsync(string path, CancellationToken cancellationToken)
    {
        await using var file = File.OpenRead(path);
        var hash = await SHA256.HashDataAsync(file, cancellationToken).ConfigureAwait(false);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>
    /// Sets the running file aside, puts the new one in its place and starts
    /// it. If the second step fails, the first is undone - otherwise no
    /// runnable program would be left behind.
    /// </summary>
    private static InstallResult PutInPlace(string ownPath, string newFile)
    {
        var setAside = ownPath + BackupSuffix;
        DeleteQuietly(setAside);

        File.Move(ownPath, setAside);

        try
        {
            File.Move(newFile, ownPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            File.Move(setAside, ownPath);
            return InstallResult.Failed(T.UpdaterReplaceFailed(ex.Message));
        }

        var start = new ProcessStartInfo(ownPath) { UseShellExecute = false };
        start.ArgumentList.Add(WaitArgument);
        start.ArgumentList.Add(Environment.ProcessId.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Process.Start(start);

        return new InstallResult(true, T.UpdaterDone);
    }

    /// <summary>
    /// Removes a set-aside earlier version. Called at startup, because only
    /// then is it no longer running.
    /// </summary>
    public static void RemovePreviousVersion()
    {
        if (Environment.ProcessPath is not { } path)
        {
            return;
        }

        if (OperatingSystem.IsMacOS())
        {
            MacOsBundle.RemovePreviousVersion(path);
            return;
        }

        DeleteQuietly(path + BackupSuffix);
    }

    private static void DeleteQuietly(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Again on the next start - no reason to fail here.
        }
    }
}
