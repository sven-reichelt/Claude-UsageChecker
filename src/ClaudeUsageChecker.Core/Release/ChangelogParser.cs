using System.Globalization;
using System.Text;

namespace ClaudeUsageChecker.Core.Release;

/// <summary>
/// Reads a "Keep a Changelog" file and returns it as a data structure.
/// </summary>
/// <remarks>
/// Deliberately not a full Markdown translator, but exactly as much as this
/// project's own changelog needs: the version headings, their subsections and
/// the bullet points inside them. Markup in the text is stripped, so that the
/// interface does not display asterisks and brackets.
///
/// The source is the bundled changelog - no network access. That makes the
/// summary available offline, and it necessarily shows what belongs to the
/// running version.
/// </remarks>
public static class ChangelogParser
{
    private const string VersionPrefix = "## ";
    private const string SectionPrefix = "### ";

    /// <summary>
    /// Reads every version whose heading carries a version number. Sections such
    /// as "[Unreleased]" are skipped.
    /// </summary>
    public static IReadOnlyList<ReleaseNotes> Parse(string markdown)
    {
        ArgumentNullException.ThrowIfNull(markdown);

        var releases = new List<ReleaseNotes>();
        var builder = new ReleaseBuilder();

        foreach (var raw in markdown.Split('\n'))
        {
            var line = raw.TrimEnd('\r');

            if (line.StartsWith(VersionPrefix, StringComparison.Ordinal))
            {
                builder.Finish(releases);
                builder.Start(line[VersionPrefix.Length..]);
                continue;
            }

            builder.Add(line);
        }

        builder.Finish(releases);
        return releases;
    }

    /// <summary>
    /// Returns the versions between two points: everything after
    /// <paramref name="after"/> up to and including <paramref name="upTo"/>,
    /// newest first.
    /// </summary>
    /// <param name="after">
    /// The version that ran last. Without it nothing counts as seen, and the
    /// whole history comes back.
    /// </param>
    /// <param name="includeAfter">
    /// Whether the entry for <paramref name="after"/> itself still counts as
    /// unseen. It does for whoever ran a pre-release of that version: the entry
    /// describes the finished release, which they have not had.
    /// </param>
    public static IReadOnlyList<ReleaseNotes> Between(
        string markdown, Version? after, Version upTo, bool includeAfter = false)
    {
        ArgumentNullException.ThrowIfNull(upTo);

        // The assembly version always has four parts, the headings in the
        // changelog three. No alignment is needed even so: Version counts a
        // missing part as -1, so "0.5.0" < "0.5.0.0" - the entry for the running
        // version therefore falls below the upper bound and no longer above the
        // version that ran last. Both bounds work out on their own. Pinned down
        // in Between_TreatsTheFourPartAssemblyVersionAsTheSameRelease.
        return [.. Parse(markdown)
            .Where(r => r.Version <= upTo
                        && (after is null || (includeAfter ? r.Version >= after : r.Version > after)))
            .OrderByDescending(r => r.Version)];
    }

    /// <summary>
    /// Extracts version and date from a heading such as "[0.5.0] – 2026-08-19".
    /// Fails when the brackets hold no version number.
    /// </summary>
    internal static bool TryParseHeading(string heading, out Version version, out DateOnly? date)
    {
        version = new Version(0, 0);
        date = null;

        var start = heading.IndexOf('[', StringComparison.Ordinal);
        var end = heading.IndexOf(']', StringComparison.Ordinal);
        if (start < 0 || end < start)
        {
            return false;
        }

        if (!Version.TryParse(heading[(start + 1)..end], out var parsed))
        {
            return false;
        }

        version = parsed;

        // Everything after the bracket is decoration; only the ISO date is wanted.
        var rest = heading[(end + 1)..].Trim(' ', '\t', '-', '–', '—', ':');
        if (DateOnly.TryParseExact(rest, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var parsedDate))
        {
            date = parsedDate;
        }

        return true;
    }

    /// <summary>
    /// Strips the markup that occurs in the changelog: bold, code spans and
    /// links. Of a link the text remains, not the address - in a window without
    /// links the address would be nothing but ballast.
    /// </summary>
    internal static string StripMarkup(string text)
    {
        var result = new StringBuilder(text.Length);

        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];

            if (c == '*' && i + 1 < text.Length && text[i + 1] == '*')
            {
                i++;
                continue;
            }

            if (c == '`')
            {
                continue;
            }

            if (c == '[' && TryReadLink(text, i, out var label, out var next))
            {
                result.Append(label);
                i = next;
                continue;
            }

            result.Append(c);
        }

        return result.ToString();
    }

    /// <summary>Reads "[text](address)" starting at <paramref name="start"/>.</summary>
    private static bool TryReadLink(string text, int start, out string label, out int end)
    {
        label = string.Empty;
        end = start;

        var close = text.IndexOf(']', start + 1);
        if (close < 0 || close + 1 >= text.Length || text[close + 1] != '(')
        {
            return false;
        }

        var paren = text.IndexOf(')', close + 2);
        if (paren < 0)
        {
            return false;
        }

        label = text[(start + 1)..close];
        end = paren;
        return true;
    }

    /// <summary>
    /// Collects the lines of one version. Kept separate because the state would
    /// otherwise have to travel across the whole reading loop.
    /// </summary>
    private sealed class ReleaseBuilder
    {
        private readonly List<ReleaseNoteSection> _sections = [];
        private readonly List<ReleaseNoteEntry> _entries = [];
        private readonly StringBuilder _current = new();

        private Version? _version;
        private DateOnly? _date;
        private string? _sectionTitle;
        private bool _currentIsContinuation;

        public void Start(string heading)
        {
            Reset();

            if (TryParseHeading(heading, out var version, out var date))
            {
                _version = version;
                _date = date;
            }
        }

        public void Add(string line)
        {
            if (_version is null)
            {
                return;
            }

            if (line.StartsWith(SectionPrefix, StringComparison.Ordinal))
            {
                CloseSection();
                _sectionTitle = StripMarkup(line[SectionPrefix.Length..]).Trim();
                return;
            }

            if (line.Length == 0)
            {
                // The blank line closes the current point. Whatever follows
                // indented is a further paragraph of that same point.
                CloseEntry();
                return;
            }

            var trimmed = line.TrimStart();
            var isBullet = trimmed.StartsWith("- ", StringComparison.Ordinal)
                           || trimmed.StartsWith("* ", StringComparison.Ordinal);

            if (isBullet)
            {
                CloseEntry();
                _currentIsContinuation = false;
                _current.Append(StripMarkup(trimmed[2..]));
                return;
            }

            if (_current.Length > 0)
            {
                // Line breaks in the source are flow, not paragraphs.
                _current.Append(' ').Append(StripMarkup(trimmed));
                return;
            }

            _currentIsContinuation = _entries.Count > 0;
            _current.Append(StripMarkup(trimmed));
        }

        public void Finish(List<ReleaseNotes> releases)
        {
            if (_version is null)
            {
                Reset();
                return;
            }

            CloseSection();

            if (_sections.Count > 0)
            {
                releases.Add(new ReleaseNotes
                {
                    Version = _version,
                    Date = _date,
                    Sections = [.. _sections]
                });
            }

            Reset();
        }

        private void CloseEntry()
        {
            if (_current.Length == 0)
            {
                return;
            }

            _entries.Add(new ReleaseNoteEntry(_current.ToString().Trim(), _currentIsContinuation));
            _current.Clear();
            _currentIsContinuation = false;
        }

        private void CloseSection()
        {
            CloseEntry();

            if (_entries.Count > 0)
            {
                _sections.Add(new ReleaseNoteSection
                {
                    Title = _sectionTitle,
                    Entries = [.. _entries]
                });
            }

            _entries.Clear();
            _sectionTitle = null;
        }

        private void Reset()
        {
            _sections.Clear();
            _entries.Clear();
            _current.Clear();
            _version = null;
            _date = null;
            _sectionTitle = null;
            _currentIsContinuation = false;
        }
    }
}
