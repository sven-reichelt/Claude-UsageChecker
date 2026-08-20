using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using ClaudeUsageChecker.Core.Localization;

namespace ClaudeUsageChecker.App.Services;

/// <summary>
/// Replacing the application on macOS, where it is a bundle rather than a file.
/// </summary>
/// <remarks>
/// <para>
/// The Windows route swaps one executable. Here a whole directory has to change
/// hands, and macOS is friendlier about it than Windows: a running bundle may
/// be moved aside, because the process holds its files open by inode and does
/// not care what the path says afterwards.
/// </para>
/// <para>
/// What is new here, and worth more than the checksum, is the signature. The
/// checksum only proves that the file is the one GitHub served; the signature
/// proves who built it. Since 0.9.0 the bundle carries a Developer ID and
/// Apple's notarisation, and both are checked before anything is put in place -
/// with the same question Gatekeeper would ask.
/// </para>
/// </remarks>
internal static class MacOsBundle
{
    /// <summary>The .app the running executable lives in, or null outside one.</summary>
    /// <remarks>
    /// By text rather than through Path: that class bends separators to the
    /// platform it runs on, and these are macOS paths whatever machine happens
    /// to be reading them.
    /// </remarks>
    internal static string? Of(string program)
    {
        for (var current = program; !string.IsNullOrEmpty(current);)
        {
            if (current.EndsWith(".app", StringComparison.OrdinalIgnoreCase))
            {
                return current;
            }

            var cut = current.LastIndexOf('/');
            if (cut <= 0)
            {
                return null;
            }

            current = current[..cut];
        }

        return null;
    }

    /// <summary>
    /// Whether the running application could replace itself: it has to live in
    /// a bundle, and that bundle's folder has to be writable.
    /// </summary>
    /// <remarks>
    /// An application in /Applications belongs to whoever installed it. Another
    /// account may be able to read it and not to replace it, and finding that
    /// out halfway through the swap would leave nothing behind.
    /// </remarks>
    internal static bool CanReplace(string? program)
    {
        if (program is null || Of(program) is not { } bundle)
        {
            return false;
        }

        var parent = Path.GetDirectoryName(bundle);
        if (string.IsNullOrEmpty(parent))
        {
            return false;
        }

        try
        {
            var probe = Path.Combine(parent, $".cuc-write-test-{Guid.NewGuid():N}");
            File.WriteAllText(probe, string.Empty);
            File.Delete(probe);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    /// <summary>
    /// Unpacks the archive and hands back the bundle inside it.
    /// </summary>
    /// <remarks>
    /// ditto rather than unzip: it keeps the flags and extended attributes the
    /// signature was made over. Unpacking with something that drops them yields
    /// a bundle that fails its own signature check.
    /// </remarks>
    internal static async Task<string?> ExtractAsync(
        string archive, string into, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(into);

        if (await RunAsync("/usr/bin/ditto", ["-xk", archive, into], cancellationToken)
                .ConfigureAwait(false) is not 0)
        {
            return null;
        }

        var bundles = Directory.GetDirectories(into, "*.app");
        return bundles.Length == 1 ? bundles[0] : null;
    }

    /// <summary>
    /// Whether macOS would run this bundle: signed, unbroken, and notarised.
    /// </summary>
    /// <remarks>
    /// Two questions, because they are two. codesign asks whether the bundle
    /// still matches its signature; spctl asks whether the signature is one the
    /// system accepts - a Developer ID, notarised, not revoked. A bundle that
    /// passes the first and fails the second is properly built and not welcome.
    /// </remarks>
    internal static async Task<bool> IsAcceptableAsync(string bundle, CancellationToken cancellationToken)
    {
        if (await RunAsync("/usr/bin/codesign", ["--verify", "--strict", bundle], cancellationToken)
                .ConfigureAwait(false) is not 0)
        {
            return false;
        }

        return await RunAsync(
            "/usr/sbin/spctl", ["--assess", "--type", "install", bundle], cancellationToken)
            .ConfigureAwait(false) is 0;
    }

    /// <summary>
    /// Puts the new bundle where the old one stands and starts it.
    /// </summary>
    /// <remarks>
    /// The old one is moved aside rather than deleted, so that a failure in the
    /// second step can be undone. It is cleared away by the version that starts
    /// next - by then nothing is running out of it.
    /// </remarks>
    internal static async Task<InstallResult> ReplaceAsync(
        string bundle, string replacement, CancellationToken cancellationToken)
    {
        var setAside = bundle + UpdateInstaller.BackupSuffix;
        DeleteDirectoryQuietly(setAside);

        Directory.Move(bundle, setAside);

        try
        {
            Directory.Move(replacement, bundle);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Directory.Move(setAside, bundle);
            return InstallResult.Failed(T.UpdaterReplaceFailed(ex.Message));
        }

        // Through open rather than by running the executable: that hands the
        // start to macOS, which then treats the program as the bundled
        // application it is. Same reasoning as the launch agent.
        var started = await RunAsync(
            "/usr/bin/open",
            ["-a", bundle, "--args", UpdateInstaller.WaitArgument,
             Environment.ProcessId.ToString(CultureInfo.InvariantCulture)],
            cancellationToken).ConfigureAwait(false);

        return started is 0
            ? new InstallResult(true, T.UpdaterDone)
            : InstallResult.Failed(T.UpdaterReplaceFailed($"open -a ({started})"));
    }

    /// <summary>Removes the bundle set aside by the version before this one.</summary>
    internal static void RemovePreviousVersion(string? program)
    {
        if (program is not null && Of(program) is { } bundle)
        {
            DeleteDirectoryQuietly(bundle + UpdateInstaller.BackupSuffix);
        }
    }

    private static void DeleteDirectoryQuietly(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Again on the next start - no reason to fail here.
        }
    }

    private static async Task<int> RunAsync(
        string program, string[] arguments, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo(program)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo);
        if (process is null)
        {
            return -1;
        }

        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        return process.ExitCode;
    }
}
