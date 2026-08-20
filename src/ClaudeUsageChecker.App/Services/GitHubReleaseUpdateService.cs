using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using ClaudeUsageChecker.App.Settings;
using ClaudeUsageChecker.Core.Localization;

namespace ClaudeUsageChecker.App.Services;

/// <summary>
/// Queries the latest release of a GitHub repository.
/// </summary>
/// <remarks>
/// Deliberately only a check with a pointer to the release page: nothing is
/// downloaded or executed automatically. Installing stays a deliberate decision
/// of the user.
/// </remarks>
public sealed class GitHubReleaseUpdateService(
    HttpClient httpClient,
    string owner,
    string repository,
    ProgramVersion currentVersion,
    Func<UpdateChannel>? channel = null) : IUpdateService
{
    public async Task<UpdateCheckResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        // Published releases have an address of their own that reports exactly
        // one. For pre-releases the whole list has to be fetched, because GitHub
        // deliberately leaves them out of "latest".
        var wanted = channel?.Invoke() ?? UpdateChannel.Stable;
        var url = wanted == UpdateChannel.PreRelease
            ? $"https://api.github.com/repos/{owner}/{repository}/releases?per_page=20"
            : $"https://api.github.com/repos/{owner}/{repository}/releases/latest";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        request.Headers.TryAddWithoutValidation("X-GitHub-Api-Version", "2022-11-28");
        request.Headers.UserAgent.Add(new ProductInfoHeaderValue(
            "ClaudeUsageChecker", currentVersion.Number.ToString(3)));

        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return UpdateCheckResult.Unavailable(
                    T.UpdateNoRelease);
            }

            if (!response.IsSuccessStatusCode)
            {
                return UpdateCheckResult.Failed(
                    T.UpdateHttpError((int)response.StatusCode));
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            using var document = JsonDocument.Parse(json);

            if (Newest(document.RootElement) is not { } root)
            {
                return UpdateCheckResult.Unavailable(T.UpdateNoRelease);
            }

            if (!root.TryGetProperty("tag_name", out var tagElement))
            {
                return UpdateCheckResult.Failed(T.UpdateIncomplete);
            }

            var tag = tagElement.GetString();
            if (!TryParseTag(tag, out var latest))
            {
                return UpdateCheckResult.Failed(T.UpdateUnknownFormat(tag ?? string.Empty));
            }

            if (latest <= currentVersion)
            {
                return UpdateCheckResult.UpToDate(currentVersion);
            }

            var page = root.TryGetProperty("html_url", out var urlElement)
                       && Uri.TryCreate(urlElement.GetString(), UriKind.Absolute, out var parsed)
                ? parsed
                : null;

            return new UpdateCheckResult
            {
                Status = UpdateCheckStatus.UpdateAvailable,
                AvailableVersion = latest,
                ReleasePage = page,
                DownloadUrl = FindAsset(root, ".exe"),
                ChecksumUrl = FindAsset(root, ".exe.sha256"),
                // Said in words, not only in the label: whoever is offered a
                // test build should know that is what it is.
                Message = latest.IsPreRelease
                    ? T.UpdateAvailablePreRelease(latest.ToString(), currentVersion.ToString())
                    : T.UpdateAvailable(latest.ToString(), currentVersion.ToString())
            };
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            return UpdateCheckResult.Failed(T.UpdateCheckFailed);
        }
    }

    /// <summary>
    /// The release to compare against: the object itself, or the newest usable
    /// one out of a list.
    /// </summary>
    /// <remarks>
    /// The address for published releases answers with a single object, the list
    /// for pre-releases with an array. Drafts are skipped - they are not
    /// published and their files are not reachable to everyone. Out of the rest
    /// the highest version wins rather than the first in the list: GitHub sorts
    /// by date of creation, and a fix released later for an older line would
    /// otherwise look newer than it is.
    /// </remarks>
    internal static JsonElement? Newest(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Array)
        {
            return root;
        }

        JsonElement? best = null;
        ProgramVersion? bestVersion = null;

        foreach (var release in root.EnumerateArray())
        {
            if (release.TryGetProperty("draft", out var draft) && draft.ValueKind == JsonValueKind.True)
            {
                continue;
            }

            if (!release.TryGetProperty("tag_name", out var tag)
                || !TryParseTag(tag.GetString(), out var version))
            {
                continue;
            }

            if (bestVersion is null || version > bestVersion)
            {
                best = release;
                bestVersion = version;
            }
        }

        return best;
    }

    /// <summary>
    /// Finds the attached file with the matching extension.
    /// </summary>
    /// <remarks>
    /// The address comes from GitHub's response for exactly this repository - it
    /// is not pieced together from the file name or guessed.
    /// </remarks>
    internal static Uri? FindAsset(JsonElement release, string suffix)
    {
        if (!release.TryGetProperty("assets", out var assets) || assets.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var asset in assets.EnumerateArray())
        {
            if (!asset.TryGetProperty("name", out var nameElement)
                || nameElement.GetString() is not { } name
                || !name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (asset.TryGetProperty("browser_download_url", out var urlElement)
                && Uri.TryCreate(urlElement.GetString(), UriKind.Absolute, out var url)
                && url.Scheme == Uri.UriSchemeHttps)
            {
                return url;
            }
        }

        return null;
    }

    /// <summary>
    /// Accepts tags of the form "v1.2.3", "1.2.3" and "v1.2.3-beta.1".
    /// </summary>
    /// <remarks>
    /// The label is kept rather than cut off: without it a pre-release and the
    /// finished version of the same number would be indistinguishable, and
    /// whoever tests one would never be offered the other.
    /// </remarks>
    internal static bool TryParseTag(string? tag, out ProgramVersion version) =>
        ProgramVersion.TryParse(tag, out version);
}
