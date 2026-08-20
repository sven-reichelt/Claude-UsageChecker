using System;
using System.Globalization;
using System.Reflection;

namespace ClaudeUsageChecker.App.Services;

/// <summary>
/// A version with the label of a pre-release, "0.7.1-beta.1" for instance.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Assembly"/>.GetName().Version cannot carry that label - it is four
/// numbers and nothing else. A pre-release and the finished version that follows
/// it would therefore look exactly alike, and whoever tested the pre-release
/// would never be offered the finished one: the check would compare 0.7.1
/// against 0.7.1 and find nothing to do. Testers would be stranded on the very
/// build that was meant to be temporary.
/// </para>
/// <para>
/// The label lives in the informational version, which the build fills from the
/// tag, and is read from there.
/// </para>
/// </remarks>
public sealed record ProgramVersion : IComparable<ProgramVersion>
{
    public ProgramVersion(Version number, string? preRelease = null)
    {
        ArgumentNullException.ThrowIfNull(number);

        Number = new Version(number.Major, number.Minor, Math.Max(number.Build, 0));
        PreRelease = string.IsNullOrWhiteSpace(preRelease) ? null : preRelease.Trim();
    }

    /// <summary>The three numbers. The fourth part of an assembly version says nothing here.</summary>
    public Version Number { get; }

    /// <summary>What stands after the hyphen, or null for a finished release.</summary>
    public string? PreRelease { get; }

    /// <summary>Whether this is a pre-release rather than a finished version.</summary>
    public bool IsPreRelease => PreRelease is not null;

    /// <summary>The version of the running program.</summary>
    public static ProgramVersion Current { get; } = Of(Assembly.GetExecutingAssembly());

    /// <summary>
    /// Reads the version out of an assembly.
    /// </summary>
    /// <remarks>
    /// The informational version first, because it is the only one carrying the
    /// label. It also carries the commit behind a "+", which
    /// <see cref="TryParse"/> drops. Without it - an assembly built without that
    /// attribute - the plain number remains.
    /// </remarks>
    public static ProgramVersion Of(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        var informational = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        return TryParse(informational, out var version)
            ? version
            : new ProgramVersion(assembly.GetName().Version ?? new Version(0, 0, 0));
    }

    /// <summary>
    /// Reads "v1.2.3-beta.1+abc123" and everything shorter than that.
    /// </summary>
    /// <remarks>
    /// The build metadata after "+" is dropped: semantic versioning explicitly
    /// leaves it out of any comparison, and here it is the commit hash, which
    /// says nothing about age.
    /// </remarks>
    public static bool TryParse(string? text, out ProgramVersion version)
    {
        version = new ProgramVersion(new Version(0, 0, 0));

        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var rest = text.Trim().TrimStart('v', 'V');

        var build = rest.IndexOf('+');
        if (build >= 0)
        {
            rest = rest[..build];
        }

        string? label = null;
        var hyphen = rest.IndexOf('-');
        if (hyphen >= 0)
        {
            label = rest[(hyphen + 1)..];
            rest = rest[..hyphen];
        }

        if (!Version.TryParse(rest, out var number))
        {
            return false;
        }

        version = new ProgramVersion(number, label);
        return true;
    }

    /// <summary>
    /// Orders two versions.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The numbers decide first. Where they match, a version <em>with</em> a
    /// label is the older one: 0.7.1-beta.1 comes before 0.7.1. That is the rule
    /// of semantic versioning, and it is the one that gets a tester off a
    /// pre-release and onto the finished version.
    /// </para>
    /// <para>
    /// Two labels are compared as text. That is enough for "beta.1" against
    /// "beta.2" and wrong for "beta.10" against "beta.2" - deliberately not
    /// solved, because the way out of a pre-release is the finished version, not
    /// counting labels. Whoever needs more numbers them "beta.02".
    /// </para>
    /// </remarks>
    public int CompareTo(ProgramVersion? other)
    {
        if (other is null)
        {
            return 1;
        }

        var byNumber = Number.CompareTo(other.Number);
        if (byNumber != 0)
        {
            return byNumber;
        }

        return (PreRelease, other.PreRelease) switch
        {
            (null, null) => 0,
            (null, _) => 1,
            (_, null) => -1,
            var (mine, theirs) => string.CompareOrdinal(mine, theirs)
        };
    }

    public static bool operator <(ProgramVersion? left, ProgramVersion? right) =>
        Comparer(left, right) < 0;

    public static bool operator >(ProgramVersion? left, ProgramVersion? right) =>
        Comparer(left, right) > 0;

    public static bool operator <=(ProgramVersion? left, ProgramVersion? right) =>
        Comparer(left, right) <= 0;

    public static bool operator >=(ProgramVersion? left, ProgramVersion? right) =>
        Comparer(left, right) >= 0;

    private static int Comparer(ProgramVersion? left, ProgramVersion? right) =>
        left is null ? (right is null ? 0 : -1) : left.CompareTo(right);

    /// <summary>"0.7.1" or "0.7.1-beta.1" - the form shown everywhere.</summary>
    public override string ToString() => IsPreRelease
        ? string.Create(CultureInfo.InvariantCulture, $"{Number.ToString(3)}-{PreRelease}")
        : Number.ToString(3);
}
