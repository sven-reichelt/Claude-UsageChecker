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
    public static string TargetDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Programs",
        "ClaudeUsageChecker");

    /// <summary>Full target path of the executable.</summary>
    public static string TargetPath { get; } = Path.Combine(TargetDirectory, "ClaudeUsageChecker.exe");

    /// <summary>Whether the running version already sits at the target location.</summary>
    public static bool IsInstalled =>
        Environment.ProcessPath is { } path
        && string.Equals(Path.GetFullPath(path), TargetPath, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Whether offering the setup makes sense at all: only for a published
    /// single file under Windows that is not at the target location yet.
    /// </summary>
    /// <remarks>
    /// The whole class is about a Windows path, and the platform used to be
    /// implied: nothing else could replace itself, so nothing else got here.
    /// Since 0.9.0 macOS can, and the implication no longer holds - it would
    /// offer to copy the program out of its bundle into a folder Windows
    /// invented. macOS has its own answer to the same question, and it is the
    /// applications folder.
    /// </remarks>
    public static bool ShouldOffer =>
        OperatingSystem.IsWindows() && UpdateInstaller.IsSupported && !IsInstalled;

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
