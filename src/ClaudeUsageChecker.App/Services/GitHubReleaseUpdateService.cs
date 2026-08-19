using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ClaudeUsageChecker.App.Services;

/// <summary>
/// Fragt die neueste Veroeffentlichung eines GitHub-Repositorys ab.
/// </summary>
/// <remarks>
/// Bewusst nur eine Pruefung mit Hinweis auf die Release-Seite: Es wird nichts
/// automatisch heruntergeladen oder ausgefuehrt. Das Einspielen bleibt eine
/// bewusste Entscheidung des Nutzers.
/// </remarks>
public sealed class GitHubReleaseUpdateService(
    HttpClient httpClient,
    string owner,
    string repository,
    Version currentVersion) : IUpdateService
{
    public async Task<UpdateCheckResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        var url = $"https://api.github.com/repos/{owner}/{repository}/releases/latest";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        request.Headers.TryAddWithoutValidation("X-GitHub-Api-Version", "2022-11-28");
        request.Headers.UserAgent.Add(new ProductInfoHeaderValue("ClaudeUsageChecker", currentVersion.ToString()));

        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return UpdateCheckResult.Unavailable(
                    "Es gibt noch keine veroeffentlichte Version zum Vergleichen.");
            }

            if (!response.IsSuccessStatusCode)
            {
                return UpdateCheckResult.Failed(
                    $"GitHub antwortete mit HTTP {(int)response.StatusCode}.");
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            if (!root.TryGetProperty("tag_name", out var tagElement))
            {
                return UpdateCheckResult.Failed("Die Antwort von GitHub war unvollstaendig.");
            }

            var tag = tagElement.GetString();
            if (!TryParseTag(tag, out var latest))
            {
                return UpdateCheckResult.Failed($"Unbekanntes Versionsformat: {tag}");
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
                Message = $"Version {UpdateCheckResult.Anzeigen(latest)} ist verfuegbar "
                          + $"(installiert: {UpdateCheckResult.Anzeigen(currentVersion)})."
            };
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            return UpdateCheckResult.Failed("Die Aktualisierungspruefung ist fehlgeschlagen.");
        }
    }

    /// <summary>
    /// Sucht die angehaengte Datei mit der passenden Endung.
    /// </summary>
    /// <remarks>
    /// Die Adresse stammt aus der Antwort von GitHub zu genau diesem
    /// Repository - sie wird nicht aus dem Dateinamen zusammengesetzt oder
    /// erraten.
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

    /// <summary>Akzeptiert Marken der Form "v1.2.3" ebenso wie "1.2.3".</summary>
    internal static bool TryParseTag(string? tag, out Version version)
    {
        version = new Version(0, 0);
        if (string.IsNullOrWhiteSpace(tag))
        {
            return false;
        }

        var trimmed = tag.TrimStart('v', 'V');
        var suffix = trimmed.IndexOfAny(['-', '+']);
        if (suffix >= 0)
        {
            trimmed = trimmed[..suffix];
        }

        return Version.TryParse(trimmed, out version!);
    }
}
