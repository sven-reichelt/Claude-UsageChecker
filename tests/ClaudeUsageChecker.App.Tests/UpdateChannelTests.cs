using System.Net;
using System.Text.Json;
using ClaudeUsageChecker.App.Services;
using ClaudeUsageChecker.App.Settings;

namespace ClaudeUsageChecker.App.Tests;

/// <summary>
/// Checks the choice between published releases and pre-releases.
/// </summary>
/// <remarks>
/// Pre-releases exist so that a version can go to a few testers before it goes
/// to everybody. GitHub deliberately leaves them out of the address for the
/// latest release, so the whole list has to be fetched and the newest picked
/// from it - by version rather than by position, because the list is sorted by
/// date of creation and a fix for an older line released later would otherwise
/// look newer than it is.
/// </remarks>
public class UpdateChannelTests
{
    [Fact]
    public void TheChannelDefaultsToPublishedReleases() =>
        Assert.Equal(UpdateChannel.Stable, new AppSettings().Channel);

    /// <summary>Anything unreadable counts as the safe side.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("beta")]
    [InlineData("Stable")]
    public void AnUnknownValueCountsAsPublishedReleases(string? stored) =>
        Assert.Equal(UpdateChannel.Stable, new AppSettings { UpdateChannel = stored }.Channel);

    [Fact]
    public void TheChannelSurvivesBeingWrittenAndReadBack()
    {
        var settings = new AppSettings { Channel = UpdateChannel.PreRelease };

        Assert.Equal("prerelease", settings.UpdateChannel);
        Assert.Equal(UpdateChannel.PreRelease, settings.Channel);
    }

    /// <summary>
    /// The channel survives the way through the file.
    /// </summary>
    /// <remarks>
    /// Worth its own test because the settings are written through a
    /// source-generated serialiser: a property it does not know about is not
    /// reported, it is simply missing from the file.
    /// </remarks>
    [Fact]
    public void TheChannelSurvivesTheSettingsFile()
    {
        var path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(), $"cuc-channel-file-{Guid.NewGuid():N}.json");

        try
        {
            var store = new SettingsStore(path);
            store.Save(new AppSettings { Channel = UpdateChannel.PreRelease });

            var text = File.ReadAllText(path);
            var loaded = new SettingsStore(path).Load();

            Assert.Contains("\"updateChannel\": \"prerelease\"", text, StringComparison.Ordinal);
            Assert.Equal(UpdateChannel.PreRelease, loaded.Channel);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    /// <summary>Out of a list the highest version wins, not the first entry.</summary>
    [Fact]
    public void TheNewestVersionWinsRatherThanTheFirstInTheList()
    {
        const string json = """
            [
              { "tag_name": "v0.6.5", "draft": false, "prerelease": false },
              { "tag_name": "v0.7.0-beta.1", "draft": false, "prerelease": true },
              { "tag_name": "v0.6.4", "draft": false, "prerelease": false }
            ]
            """;

        var newest = Newest(json);

        Assert.Equal("v0.7.0-beta.1", newest.GetProperty("tag_name").GetString());
    }

    /// <summary>
    /// Drafts are skipped: they are not published, and their files cannot be
    /// reached by everyone.
    /// </summary>
    [Fact]
    public void ADraftIsSkipped()
    {
        const string json = """
            [
              { "tag_name": "v0.9.0", "draft": true, "prerelease": false },
              { "tag_name": "v0.6.5", "draft": false, "prerelease": false }
            ]
            """;

        Assert.Equal("v0.6.5", Newest(json).GetProperty("tag_name").GetString());
    }

    [Fact]
    public void AListWithNothingUsableYieldsNothing()
    {
        const string json = """[ { "tag_name": "v1.0.0", "draft": true } ]""";

        using var document = JsonDocument.Parse(json);

        Assert.Null(GitHubReleaseUpdateService.Newest(document.RootElement));
    }

    /// <summary>A single object is passed through - that is the published route.</summary>
    [Fact]
    public void ASingleReleaseIsTakenAsItIs()
    {
        const string json = """{ "tag_name": "v0.6.4", "draft": false }""";

        Assert.Equal("v0.6.4", Newest(json).GetProperty("tag_name").GetString());
    }

    /// <summary>
    /// The channel decides which address is asked, and it is read at the moment
    /// of the check.
    /// </summary>
    /// <remarks>
    /// Read afresh each time rather than captured once: whoever switches to
    /// pre-releases expects the next check to follow, not the next start.
    /// </remarks>
    [Theory]
    [InlineData(UpdateChannel.Stable, "releases/latest")]
    [InlineData(UpdateChannel.PreRelease, "releases?per_page=")]
    public async Task TheChannelDecidesWhichAddressIsAsked(UpdateChannel channel, string expected)
    {
        var handler = new RecordingHandler();
        using var client = new HttpClient(handler);
        var service = new GitHubReleaseUpdateService(
            client, "owner", "repository", new ProgramVersion(new Version(0, 6, 4)), () => channel);

        await service.CheckAsync();

        Assert.Contains(expected, handler.Url ?? string.Empty, StringComparison.Ordinal);
    }

    private static JsonElement Newest(string json)
    {
        using var document = JsonDocument.Parse(json);
        var element = GitHubReleaseUpdateService.Newest(document.RootElement);

        Assert.NotNull(element);

        return element.Value.Clone();
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public string? Url { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Url = request.RequestUri?.ToString();

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{ "tag_name": "v0.6.4", "draft": false }""")
            });
        }
    }
}
