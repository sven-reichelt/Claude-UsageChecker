using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using ClaudeUsageChecker.App.Views;

using ClaudeUsageChecker.Core.Localization;

namespace ClaudeUsageChecker.App.Services;

/// <summary>
/// Moves the application to a permanent location inside the user profile and
/// sets up autostart.
/// </summary>
/// <remarks>
/// <para>
/// The reason is not tidiness: autostart and the pinned tray icon both depend
/// on the path of the executable. If it sits in the downloads folder, both break
/// as soon as that folder is cleaned out. The self-update writes to exactly that
/// path as well.
/// </para>
/// <para>
/// Copying happens only after asking. A downloaded program that writes itself
/// elsewhere and registers for autostart unasked would be overbearing - however
/// useful it may be.
/// </para>
/// </remarks>
public static class SelfInstaller
{
    /// <summary>
    /// Directory the application is meant to live in permanently.
    /// </summary>
    /// <remarks>
    /// <c>%LOCALAPPDATA%\Programs</c> is the location Windows intends for
    /// applications that manage without administrator rights - VS Code and Signal
    /// live there too. It keeps the root of the user profile clear, where nobody
    /// expects programs next to documents and downloads.
    /// </remarks>
    public static string TargetDirectory { get; } =
        OperatingSystem.IsMacOS()
            ? "/Applications"
            : Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Programs",
                "ClaudeUsageChecker");

    /// <summary>
    /// What is put there: the executable on Windows, the whole bundle on macOS.
    /// </summary>
    /// <remarks>
    /// Written out rather than composed with <see cref="Path.Combine"/>. That
    /// bends the separator to the platform it runs on - tests included, and
    /// these run on Windows, where a macOS path would come out carrying a
    /// backslash.
    /// </remarks>
    public static string TargetPath { get; } =
        OperatingSystem.IsMacOS()
            ? "/Applications/ClaudeUsageChecker.app"
            : Path.Combine(TargetDirectory, "ClaudeUsageChecker.exe");

    /// <summary>The executable inside the installed bundle. macOS only.</summary>
    /// <remarks>
    /// The launch agent wants the program, not the bundle - it derives the
    /// bundle itself, because <c>open -a</c> refuses a bare executable and
    /// launchd never complains about an agent that fails.
    /// </remarks>
    internal static string TargetProgram =>
        TargetPath + "/Contents/MacOS/ClaudeUsageChecker";

    /// <summary>Whether the running version already sits at the target location.</summary>
    public static bool IsInstalled =>
        Environment.ProcessPath is { } path
        && string.Equals(RunningFrom(path), TargetPath, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Whether offering the setup makes sense at all.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The whole class used to be about a Windows path, and the platform was
    /// merely implied: nothing else could replace itself, so nothing else got
    /// here. Since 0.9.0 macOS can, and the implication no longer held - it
    /// would have offered to copy the program out of its bundle into a folder
    /// Windows invented. macOS answered the same question decades earlier, and
    /// the answer is the applications folder.
    /// </para>
    /// <para>
    /// Deliberately <em>not</em> asked on macOS: whether this copy could replace
    /// itself where it stands. That is <see cref="UpdateInstaller.IsSupported"/>,
    /// which asks whether the folder around the bundle can be written to. A disk
    /// image is read-only, so it says no - and the one place the offer matters
    /// most is the place it would never have appeared. What decides is whether
    /// the <em>target</em> can be written to.
    /// </para>
    /// </remarks>
    public static bool ShouldOffer =>
        !IsInstalled
        && (OperatingSystem.IsMacOS()
            ? Environment.ProcessPath is { } path
                && MacOsBundle.Of(path) is not null
                && CanWriteToTarget()
            : OperatingSystem.IsWindows() && UpdateInstaller.IsSupported);

    /// <summary>Whether this copy is running from a mounted disk image.</summary>
    /// <remarks>
    /// The setup says different things in the two cases, and only one of them
    /// ends with something being ejected. Promising an ejection to someone who
    /// started the program out of their downloads folder would be a small lie
    /// that the next sentence does not make good.
    /// </remarks>
    public static bool RunsFromDiskImage =>
        OperatingSystem.IsMacOS()
        && Environment.ProcessPath is { } path
        && MacOsBundle.Of(path) is { } bundle
        && VolumeOf(bundle) is not null;

    /// <summary>Where this copy lives: the file, or the bundle around it.</summary>
    private static string? RunningFrom(string path) =>
        OperatingSystem.IsMacOS() ? MacOsBundle.Of(path) : Path.GetFullPath(path);

    /// <summary>
    /// Whether the applications folder takes what we would put there.
    /// </summary>
    /// <remarks>
    /// Asked rather than assumed: on a machine whose /Applications belongs to
    /// another account, the copy would fail halfway. Better not to offer than to
    /// offer and fail.
    /// </remarks>
    private static bool CanWriteToTarget()
    {
        try
        {
            var probe = Path.Combine(TargetDirectory, $".cuc-write-test-{Guid.NewGuid():N}");
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
    /// Copies the running file to the target location, sets up autostart and
    /// starts it from there. On success the caller has to end itself - the new
    /// instance is already waiting for that.
    /// </summary>
    public static InstallResult Install()
    {
        if (Environment.ProcessPath is not { Length: > 0 } source)
        {
            return InstallResult.Failed(T.InstallerLocationUnknown);
        }

        if (IsInstalled)
        {
            return new InstallResult(true, T.InstallerAlreadyInPlace);
        }

        return OperatingSystem.IsMacOS() ? InstallBundle(source) : InstallFile(source);
    }

    /// <summary>
    /// Copies the bundle into the applications folder, starts it from there, and
    /// leaves the ejecting to the copy.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>ditto</c> rather than a directory copy, and that is not a preference.
    /// Most of this application is managed .NET assemblies, which are not Mach-O
    /// files, so codesign cannot store their signatures inside them and puts
    /// them in extended attributes beside them. A copy that drops those installs
    /// a bundle that fails its own signature check - notarised, in place, and
    /// refused by macOS with a message about malware. .NET's file copying knows
    /// nothing of extended attributes; ditto exists for this.
    /// </para>
    /// <para>
    /// The ejecting is the copy's job, not ours: this process is still running
    /// from the volume, and a volume with a running process on it does not
    /// detach.
    /// </para>
    /// </remarks>
    private static InstallResult InstallBundle(string source)
    {
        if (MacOsBundle.Of(source) is not { } bundle)
        {
            return InstallResult.Failed(T.InstallerLocationUnknown);
        }

        var setAside = TargetPath + UpdateInstaller.BackupSuffix;
        var replacing = Directory.Exists(TargetPath);

        try
        {
            if (replacing)
            {
                // ditto writes over what it finds but removes nothing, so a
                // leftover file of an older version would survive into the new
                // one - and break its signature, which seals the file list.
                DeleteDirectoryQuietly(setAside);
                Directory.Move(TargetPath, setAside);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return InstallResult.Failed(T.InstallerCopyFailed(ex.Message));
        }

        if (Run("/usr/bin/ditto", [bundle, TargetPath]) is not 0)
        {
            if (replacing && !Directory.Exists(TargetPath))
            {
                Directory.Move(setAside, TargetPath);
            }

            return InstallResult.Failed(T.InstallerCopyFailed("ditto"));
        }

        AutostartManager.Apply(enabled: true, TargetProgram);

        if (Run("/usr/bin/open", StartArguments(bundle)) is not 0)
        {
            return InstallResult.Failed(T.InstallerStartFailed("open -n -a"));
        }

        return new InstallResult(true, T.InstallerDone);
    }

    /// <summary>
    /// How the installed copy is started: a new instance, told to wait for this
    /// one and then to eject what it came from.
    /// </summary>
    internal static string[] StartArguments(string bundle)
    {
        var arguments = MacOsBundle.StartArguments(TargetPath, Environment.ProcessId);

        return VolumeOf(bundle) is { } volume
            ? [.. arguments, UpdateInstaller.EjectArgument, volume]
            : arguments;
    }

    /// <summary>The mounted volume a path lies on, if it lies on one at all.</summary>
    /// <remarks>
    /// Run from the downloads folder rather than from an image, there is nothing
    /// to eject and nothing to say about it - hence null rather than a failure.
    /// </remarks>
    internal static string? VolumeOf(string path)
    {
        const string Volumes = "/Volumes/";

        if (path is null || !path.StartsWith(Volumes, StringComparison.Ordinal))
        {
            return null;
        }

        var rest = path.AsSpan(Volumes.Length);
        var end = rest.IndexOf('/');
        var name = end < 0 ? rest : rest[..end];

        return name.IsEmpty ? null : Volumes + name.ToString();
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
            // A leftover costs disk space and nothing else. The next start tries
            // again.
        }
    }

    private static int Run(string program, string[] arguments)
    {
        try
        {
            var start = new ProcessStartInfo(program) { UseShellExecute = false };
            foreach (var argument in arguments)
            {
                start.ArgumentList.Add(argument);
            }

            using var process = Process.Start(start);
            if (process is null)
            {
                return -1;
            }

            process.WaitForExit((int)TimeSpan.FromMinutes(2).TotalMilliseconds);
            return process.HasExited ? process.ExitCode : -1;
        }
        catch (Exception ex)
            when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            return -1;
        }
    }

    private static InstallResult InstallFile(string source)
    {
        try
        {
            Directory.CreateDirectory(TargetDirectory);
            File.Copy(source, TargetPath, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return InstallResult.Failed(T.InstallerCopyFailed(ex.Message));
        }

        // Autostart deliberately points at the target path, not the current one -
        // otherwise it would keep pointing into the downloads folder.
        AutostartManager.Apply(enabled: true, TargetPath);

        var start = new ProcessStartInfo(TargetPath) { UseShellExecute = false };
        start.ArgumentList.Add(UpdateInstaller.WaitArgument);
        start.ArgumentList.Add(Environment.ProcessId.ToString(CultureInfo.InvariantCulture));

        try
        {
            Process.Start(start);
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            return InstallResult.Failed(T.InstallerStartFailed(ex.Message));
        }

        return new InstallResult(true, T.InstallerDone);
    }
}
