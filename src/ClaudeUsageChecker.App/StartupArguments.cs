using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using ClaudeUsageChecker.App.Services;

namespace ClaudeUsageChecker.App;

/// <summary>
/// Interprets the command line. The only switch serves the restart after an
/// update.
/// </summary>
internal static class StartupArguments
{
    /// <summary>
    /// How long the predecessor is waited for. Waiting longer does not help - by
    /// then something else is wrong, and the single-instance lock catches it as a
    /// last resort anyway.
    /// </summary>
    private static readonly TimeSpan MaxWait = TimeSpan.FromSeconds(30);

    /// <summary>
    /// After an update, waits for the replaced version to end - otherwise this
    /// instance fails on the single-instance lock and the user would be left
    /// without a running application after updating.
    /// </summary>
    public static void WaitForPredecessor(string[] args)
    {
        if (TryReadPredecessorId(args) is not { } processId)
        {
            return;
        }

        try
        {
            using var predecessor = Process.GetProcessById(processId);
            predecessor.WaitForExit((int)MaxWait.TotalMilliseconds);
        }
        catch (ArgumentException)
        {
            // Already gone - which was exactly the aim.
        }
        catch (InvalidOperationException)
        {
            // Likewise.
        }

        // The lock is only released on exit; a brief grace period avoids a race
        // for it.
        Thread.Sleep(TimeSpan.FromMilliseconds(500));
    }

    /// <summary>
    /// Ejects the disk image the application was installed from, if it was.
    /// </summary>
    /// <remarks>
    /// Called after the predecessor has ended, because until then the volume is
    /// in use by it and will not detach. Failure is silent on purpose: an image
    /// left mounted is untidy, not broken, and the alternative would be an error
    /// message about a disk on the first start of a fresh installation.
    /// </remarks>
    public static void EjectSourceVolume(string[] args)
    {
        if (!OperatingSystem.IsMacOS() || TryReadSourceVolume(args) is not { } volume)
        {
            return;
        }

        try
        {
            var start = new ProcessStartInfo("/usr/bin/hdiutil") { UseShellExecute = false };
            start.ArgumentList.Add("detach");
            start.ArgumentList.Add(volume);
            start.ArgumentList.Add("-quiet");

            using var process = Process.Start(start);
            process?.WaitForExit((int)TimeSpan.FromSeconds(20).TotalMilliseconds);
        }
        catch (Exception ex)
            when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            // See above: untidy, not broken.
        }
    }

    /// <summary>Reads the volume to eject from the command line.</summary>
    internal static string? TryReadSourceVolume(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        for (var i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], UpdateInstaller.EjectArgument, StringComparison.Ordinal)
                && args[i + 1] is { Length: > 0 } volume)
            {
                return volume;
            }
        }

        return null;
    }

    /// <summary>Reads the id of the predecessor instance from the command line.</summary>
    internal static int? TryReadPredecessorId(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        for (var i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], UpdateInstaller.WaitArgument, StringComparison.Ordinal)
                && int.TryParse(args[i + 1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var id)
                && id > 0)
            {
                return id;
            }
        }

        return null;
    }
}
