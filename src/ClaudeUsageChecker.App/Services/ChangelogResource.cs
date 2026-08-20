using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Platform;
using ClaudeUsageChecker.Core.Localization;
using ClaudeUsageChecker.Core.Release;

namespace ClaudeUsageChecker.App.Services;

/// <summary>
/// Keeps the bundled changelog at hand, in the language that is set.
/// </summary>
/// <remarks>
/// <para>
/// The files are resources inside the program, not fetched from the network.
/// Two reasons: the summary is available without a connection, and it
/// necessarily shows the state belonging to the running version - not that of a
/// later repository.
/// </para>
/// <para>
/// English is the source and lives in the root of the repository; the
/// translations sit under <c>docs/changelog/</c>. Where one is missing, the
/// English version steps in and the display says so openly - an empty window
/// would be the worse answer.
/// </para>
/// </remarks>
public static class ChangelogResource
{
    private static readonly Dictionary<string, string> Cache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Returns the changelog in the requested language, and says whether it
    /// really is the requested one.
    /// </summary>
    public static (string Text, bool IsTranslated) Read(Language? language = null)
    {
        var selected = language ?? Localizer.Current.Language;

        if (selected.Code != Language.Default.Code
            && ReadResource($"avares://ClaudeUsageChecker/Assets/changelog/{selected.Code}.md") is { Length: > 0 } text)
        {
            return (text, true);
        }

        return (ReadResource("avares://ClaudeUsageChecker/Assets/CHANGELOG.md"),
            selected.Code == Language.Default.Code);
    }

    /// <summary>
    /// The changes between the version that ran last and the current one.
    /// </summary>
    public static IReadOnlyList<ReleaseNotes> Between(ProgramVersion? lastRun, ProgramVersion current)
    {
        ArgumentNullException.ThrowIfNull(current);

        // Whoever ran a pre-release has not seen the entry of that version as a
        // release - it belongs in the span rather than counting as read.
        return ChangelogParser.Between(
            Read().Text, lastRun?.Number, current.Number,
            includeAfter: lastRun?.IsPreRelease == true);
    }

    /// <summary>The whole changelog, newest version first.</summary>
    public static IReadOnlyList<ReleaseNotes> All() =>
        ChangelogParser.Between(Read().Text, after: null, upTo: new Version(int.MaxValue, 0));

    /// <summary>
    /// The entry for exactly one version.
    /// </summary>
    /// <remarks>
    /// For the case where the version that ran before is unknown: no span can be
    /// formed then, and the whole changelog would be too much.
    /// </remarks>
    public static IReadOnlyList<ReleaseNotes> Only(Version version)
    {
        ArgumentNullException.ThrowIfNull(version);
        return [.. All().Where(r => r.Version == version)];
    }

    /// <summary>Whether the changelog exists in the language that is set.</summary>
    public static bool IsTranslated => Read().IsTranslated;

    private static string ReadResource(string uri)
    {
        if (Cache.TryGetValue(uri, out var cached))
        {
            return cached;
        }

        string text;
        try
        {
            using var stream = AssetLoader.Open(new Uri(uri));
            using var reader = new StreamReader(stream);
            text = reader.ReadToEnd();
        }
        catch (Exception ex) when (ex is FileNotFoundException or IOException)
        {
            // Without the file only the summary is lost - no reason to let the
            // start fail.
            text = string.Empty;
        }

        Cache[uri] = text;
        return text;
    }
}
