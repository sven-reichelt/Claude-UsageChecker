using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using ClaudeUsageChecker.App.Services;
using ClaudeUsageChecker.App.Settings;
using ClaudeUsageChecker.App.Tray;
using ClaudeUsageChecker.App.Views;
using ClaudeUsageChecker.Core.Authentication;
using ClaudeUsageChecker.Core.Authentication.OAuth;
using ClaudeUsageChecker.Core.Localization;
using ClaudeUsageChecker.Core.Models;
using ClaudeUsageChecker.Core.Services;
using T = ClaudeUsageChecker.Core.Localization.T;

namespace ClaudeUsageChecker.App.Tests;

/// <summary>
/// Draws the pictures of the user guide, in every language.
/// </summary>
/// <remarks>
/// <para>
/// Run as a test, it only checks that every scene draws. With
/// <c>CUC_GUIDE_DIR</c> pointing at <c>docs/guide/images</c> it writes the
/// pictures there, one folder per language:
/// </para>
/// <code>
/// $env:CUC_GUIDE_DIR = "$PWD\docs\guide\images"
/// dotnet test tests/ClaudeUsageChecker.App.Tests --filter GuideScreenshots
/// </code>
/// <para>
/// Everything shown is made up. The guide is public, and a picture taken on a
/// real machine carries that machine along: the path of the profile in the
/// setup window, the expiry of a real sign-in in the settings. So the Claude
/// Code token, the own sign-in and the target path are all handed in here
/// rather than read.
/// </para>
/// </remarks>
public class GuideScreenshots : IDisposable
{
    private readonly Language _before = Localizer.Current.Language;

    public void Dispose()
    {
        Localizer.Use(_before);
        GC.SuppressFinalize(this);
    }

    public static TheoryData<string> Languages()
    {
        var data = new TheoryData<string>();
        foreach (var language in Language.All)
        {
            data.Add(language.Code);
        }

        return data;
    }

    /// <summary>The file names the guides refer to, in the order they appear.</summary>
    internal static readonly string[] Scenes =
    [
        "01-setup", "02-sign-in", "03-menu", "04-details", "05-notice",
        "06-settings", "07-update", "08-whats-new", "09-about"
    ];

    [AvaloniaTheory]
    [MemberData(nameof(Languages))]
    public void EveryPictureOfTheGuideDraws(string code)
    {
        Localizer.Use(Language.Find(code)!);
        var now = DateTimeOffset.Now;

        Capture(Setup(), code, "01-setup");
        Capture(new SignInWindow(new AnthropicOAuthClient(new HttpClient()), SignedOutStore()), code, "02-sign-in");
        Capture(Menu(now), code, "03-menu");
        Capture(Details(now), code, "04-details");
        Capture(Notice(now), code, "05-notice");

        var settingsPath = Path.Combine(Path.GetTempPath(), $"cuc-guide-{Guid.NewGuid():N}.json");
        try
        {
            Capture(Settings(settingsPath, now), code, "06-settings");
        }
        finally
        {
            File.Delete(settingsPath);
        }

        Capture(Update(), code, "07-update");
        Capture(WhatsNew(), code, "08-whats-new");
        Capture(new AboutWindow(App.RepositoryUri, new ProgramVersion(new Version(1, 1, 0))), code, "09-about");
    }

    private static InstallPromptWindow Setup()
    {
        var window = new InstallPromptWindow();

        // The real one names the profile of whoever renders it.
        window.FindControl<TextBlock>("TargetText")!.Text =
            @"%LOCALAPPDATA%\Programs\ClaudeUsageChecker\ClaudeUsageChecker.exe";

        return window;
    }

    private static TrayMenuWindow Menu(DateTimeOffset now)
    {
        var window = new TrayMenuWindow();
        window.Render(
            TrayIconController.BuildStatusLines(State(now), now),
            [
                (T.TrayRefreshNow, () => { }),
                (T.TraySettings, () => { }),
                (T.TrayCheckForUpdates, () => { }),
                (T.TrayAbout("1.1.0"), () => { }),
                (T.TrayExit, () => { })
            ]);

        return window;
    }

    private static DetailsWindow Details(DateTimeOffset now)
    {
        var window = new DetailsWindow();
        window.Render(State(now));
        return window;
    }

    private static UsageAlertWindow Notice(DateTimeOffset now)
    {
        var window = new UsageAlertWindow(new UsageAlertBehaviour(true, true, TimeSpan.FromSeconds(30)));
        window.Add(
        [
            new UsageAlert(UsageAlertLimit.Session, null, UsageAlertLevel.Warning,
                new UsageWindow(78, now.AddHours(2).AddMinutes(14)), 75),
            new UsageAlert(UsageAlertLimit.Weekly, null, UsageAlertLevel.Critical,
                new UsageWindow(91, now.AddDays(3).AddHours(5)), 90)
        ]);

        return window;
    }

    private static SettingsWindow Settings(string path, DateTimeOffset now)
    {
        var ownSignIn = new OAuthTokenStore(new FakeSecretStore());
        ownSignIn.Write(new OAuthTokens
        {
            AccessToken = "example",
            RefreshToken = "example",
            ExpiresAt = now.AddHours(7).AddMinutes(40),
            Scope = "user:profile"
        });

        return new SettingsWindow(
            new SettingsStore(path),
            new AppSettings { LaunchAtLogin = true },
            ownSignIn,
            applyAutostart: _ => { },
            readClaudeCodeToken: () => Task.FromResult<AccessToken?>(
                new AccessToken("example", TokenSource.ClaudeCli, now.AddHours(5).AddMinutes(20))),
            plan: ExamplePlan);
    }

    private static UpdateAvailableWindow Update() => new(
        new UpdateCheckResult
        {
            Status = UpdateCheckStatus.UpdateAvailable,
            AvailableVersion = new ProgramVersion(new Version(1, 1, 1)),
            ReleasePage = new Uri("https://example.invalid/releases/v1.1.1"),
            DownloadUrl = new Uri("https://example.invalid/ClaudeUsageChecker.exe"),
            ChecksumUrl = new Uri("https://example.invalid/ClaudeUsageChecker.exe.sha256"),
            Message = T.UpdateAvailable("1.1.1", "1.1.0")
        },
        canInstall: true);

    private static ReleaseNotesWindow WhatsNew()
    {
        var window = new ReleaseNotesWindow();
        window.Render(
            ChangelogResource.Only(new Version(1, 1, 0)),
            new ProgramVersion(new Version(1, 0, 2)),
            ChangelogResource.IsTranslated,
            new ProgramVersion(new Version(1, 1, 0)));

        return window;
    }

    private static OAuthTokenStore SignedOutStore() => new(new FakeSecretStore());

    /// <summary>
    /// The one plan that has been measured, so the pictures show nothing assumed.
    /// </summary>
    private static readonly SubscriptionPlan ExamplePlan =
        new("claude_max", "default_claude_max_5x", HasClaudeMax: true);

    /// <summary>A week well under way: the session at ease, the week in the yellow.</summary>
    private static UsageState State(DateTimeOffset now) => new()
    {
        Kind = UsageStateKind.Ready,
        Snapshot = new UsageSnapshot
        {
            Session = new UsageWindow(38, now.AddHours(2).AddMinutes(14)),
            Weekly = new UsageWindow(81, now.AddDays(3).AddHours(5)),
            ScopedWeekly = [new ScopedUsageWindow("Fable", new UsageWindow(12, now.AddDays(3).AddHours(5)))],
            ExtraUsage = new ExtraUsage(
                IsEnabled: true, Used: 12.40m, Limit: 50m, Utilization: 25d,
                Currency: "EUR", Decimals: 2),
            Plan = ExamplePlan,
            RetrievedAt = now,
            TokenSource = TokenSource.OAuth
        }
    };

    private static void Capture(Window window, string code, string scene)
    {
        window.Show();

        var frame = window.CaptureRenderedFrame();
        Assert.NotNull(frame);
        Assert.True(frame.Size.Width > 0 && frame.Size.Height > 0, $"{scene} in {code} rendered an empty frame.");

        if (Environment.GetEnvironmentVariable("CUC_GUIDE_DIR") is { Length: > 0 } root)
        {
            var directory = Path.Combine(root, code);
            Directory.CreateDirectory(directory);
            frame.Save(Path.Combine(directory, scene + ".png"));
        }

        window.Close();
    }
}
