using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace ClaudeUsageChecker.App.Services;

/// <summary>
/// Removes the extraction folders of earlier versions from the temporary
/// directory.
/// </summary>
/// <remarks>
/// <para>
/// A compressed single file cannot load its native libraries from the bundle -
/// the .NET runtime extracts them at startup to
/// <c>%TEMP%\.net\&lt;application&gt;\&lt;id&gt;</c>. The id depends on the content, so every
/// new version creates a folder of its own. Nothing is cleaned up in the
/// process: left alone, the directory grows by some 16 MB per update.
/// </para>
/// <para>
/// The application's own folder is recognised from the loaded modules, not
/// through <c>AppContext.BaseDirectory</c>. For a single file the latter points at
/// the directory of the executable rather than at the extraction - relying on it
/// deletes the very folder the running application loads its libraries from.
/// </para>
/// <para>
/// Where the own folder cannot be determined, nothing is deleted. A cleaner
/// that strikes when in doubt does more damage than the disk space it frees.
/// </para>
/// </remarks>
public static class TempCleanup
{
    private const string AppFolderPrefix = "ClaudeUsageChecker";

    /// <summary>Cleans up and reports how many folders were removed.</summary>
    public static int RemoveStaleExtractions()
    {
        var root = Path.Combine(Path.GetTempPath(), ".net");
        if (!Directory.Exists(root))
        {
            return 0;
        }

        var own = FindOwnExtractions(root);
        if (own.Count == 0)
        {
            // Without knowing our own folder, better to touch nothing.
            return 0;
        }

        var removed = 0;

        foreach (var appFolder in DirectoriesOrEmpty(root))
        {
            if (!Path.GetFileName(appFolder).StartsWith(AppFolderPrefix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            foreach (var extraction in DirectoriesOrEmpty(appFolder))
            {
                if (own.Contains(Normalise(extraction)))
                {
                    continue;
                }

                if (DeleteQuietly(extraction))
                {
                    removed++;
                }
            }

            if (DirectoriesOrEmpty(appFolder).Length == 0
                && FilesOrEmpty(appFolder).Length == 0)
            {
                DeleteQuietly(appFolder);
            }
        }

        return removed;
    }

    /// <summary>
    /// The extraction folders this process has actually loaded libraries from.
    /// That is the reliable answer to what is in use.
    /// </summary>
    private static HashSet<string> FindOwnExtractions(string root)
    {
        var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var normalisedRoot = Normalise(root);

        try
        {
            foreach (ProcessModule module in Process.GetCurrentProcess().Modules)
            {
                if (module.FileName is not { Length: > 0 } file)
                {
                    continue;
                }

                var folder = Path.GetDirectoryName(file);
                if (folder is null || !Normalise(folder).StartsWith(normalisedRoot, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                found.Add(Normalise(folder));
            }
        }
        catch (Exception ex) when (ex is InvalidOperationException or NotSupportedException)
        {
            // No module list, no answer - then nothing is cleaned up.
        }

        return found;
    }

    private static string Normalise(string path) =>
        Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));

    private static string[] DirectoriesOrEmpty(string path)
    {
        try
        {
            return Directory.GetDirectories(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return [];
        }
    }

    private static string[] FilesOrEmpty(string path)
    {
        try
        {
            return Directory.GetFiles(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return [];
        }
    }

    private static bool DeleteQuietly(string path)
    {
        try
        {
            Directory.Delete(path, recursive: true);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // In use or locked - try again on the next start.
            return false;
        }
    }
}
