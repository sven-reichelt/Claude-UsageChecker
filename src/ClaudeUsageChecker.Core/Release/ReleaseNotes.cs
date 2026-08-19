namespace ClaudeUsageChecker.Core.Release;

/// <summary>The changes of a single version, prepared for display.</summary>
public sealed record ReleaseNotes
{
    public required Version Version { get; init; }

    /// <summary>Release date, where the changelog names one.</summary>
    public DateOnly? Date { get; init; }

    public required IReadOnlyList<ReleaseNoteSection> Sections { get; init; }
}

/// <summary>
/// A subsection of a version - a "### Fixed" line in the changelog. Without a
/// preceding heading, <see cref="Title"/> stays empty.
/// </summary>
public sealed record ReleaseNoteSection
{
    public string? Title { get; init; }

    public required IReadOnlyList<ReleaseNoteEntry> Entries { get; init; }
}

/// <summary>
/// One bullet point. An indented paragraph without a bullet of its own belongs
/// to the preceding point and is carried as <see cref="IsContinuation"/> - the
/// display then leaves out the bullet instead of feigning a second point.
/// </summary>
public sealed record ReleaseNoteEntry(string Text, bool IsContinuation = false);
