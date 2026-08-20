using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using ClaudeUsageChecker.App.Services;
using ClaudeUsageChecker.App.Settings;
using ClaudeUsageChecker.App.Tray;
using ClaudeUsageChecker.App.Views;
using ClaudeUsageChecker.Core.Api;
using ClaudeUsageChecker.Core.Authentication;
using ClaudeUsageChecker.Core.Authentication.OAuth;
using ClaudeUsageChecker.Core.Configuration;
using ClaudeUsageChecker.Core.Localization;
using ClaudeUsageChecker.Core.Platform;
using ClaudeUsageChecker.Core.Release;
using ClaudeUsageChecker.Core.Services;

namespace ClaudeUsageChecker.App;

/// <summary>
/// Entry point and composition root. The application has no main window; it
/// lives entirely in the system tray.
/// </summary>
public partial class App : Application, IDisposable
{
    private readonly SettingsStore _settingsStore = new();
    private readonly ISecretStore _secretStore = CreateSecretStore();

    private AppSettings _settings = new();
    private HttpClient? _usageHttpClient;
    private HttpClient? _updateHttpClient;
    private HttpClient? _oauthHttpClient;
    private UsageMonitor? _monitor;
    private TrayIconController? _tray;
    private DetailsWindow? _detailsWindow;
    private IUpdateService? _updateService;
    private AnthropicOAuthClient? _oauthClient;
    private OAuthTokenStore? _oauthTokenStore;
    private OAuthTokenProvider? _oauthTokenProvider;
    private UpdateCheckResult? _pendingUpdate;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Without a main window, shutting down has to be explicit.
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            desktop.Exit += (_, _) => Shutdown();

            _settings = _settingsStore.Load();

            // Before anything else: every window and every menu entry fetches
            // its label from the localizer, which therefore has to be on the
            // right language already.
            Localizer.Use(Language.Find(_settings.Language) ?? Language.FromSystem());

            Compose();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void Compose()
    {
        var options = new UsageApiOptions();

        _usageHttpClient = new HttpClient
        {
            BaseAddress = options.BaseAddress,
            Timeout = options.Timeout
        };

        // The application's own sign-in gets its own HttpClient: it talks to
        // console.anthropic.com, not to the base address of the usage call.
        _oauthHttpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        _oauthClient = new AnthropicOAuthClient(_oauthHttpClient);
        _oauthTokenStore = new OAuthTokenStore(_secretStore);
        _oauthTokenProvider = new OAuthTokenProvider(_oauthTokenStore, _oauthClient);

        // When the application's own sign-in expires, the call does fall back to
        // Claude Code - but the user should learn about it rather than lose the
        // independence unnoticed.
        _oauthTokenProvider.SignInExpired += (_, reason) => Dispatcher.UIThread.Post(
            () => ErrorGuard.Run("notice about expired sign-in", () => ShowSignInExpired(reason)));

        var apiClient = new AnthropicUsageApiClient(_usageHttpClient, BuildTokenProviders(), options);

        // The thresholds are none of the monitor's business - it fetches values
        // and does not judge them. Judging happens in TrayIconSeverityResolver,
        // which reads the settings afresh on every draw; changed thresholds
        // therefore take effect at once, without a restart.
        _monitor = new UsageMonitor(apiClient, new MonitorOptions
        {
            PollInterval = _settings.PollInterval
        });

        _updateHttpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        _updateService = CreateUpdateService(_updateHttpClient);

        // Every action from the tray goes through the guard: a failure in one
        // must not end the application without a word.
        _tray = new TrayIconController(_monitor, () => _settings);
        _tray.ShowDetails += (_, _) => ErrorGuard.Run("show details", ShowDetails);
        _tray.ShowSettings += (_, _) => ErrorGuard.Run("open settings", ShowSettings);
        _tray.RefreshRequested += (_, _) => ErrorGuard.Forget("trigger a call", RefreshAsync);
        _tray.CheckForUpdatesRequested += (_, _) => ErrorGuard.Forget(
            "update check", () => CheckForUpdatesAsync(announceUpToDate: true));
        _tray.ShowAboutRequested += (_, _) => ErrorGuard.Run("open the about window", ShowAbout);
        _tray.ExitRequested += (_, _) => ErrorGuard.Run("exit", RequestShutdown);

        _monitor.StateChanged += (_, state) => Dispatcher.UIThread.Post(
            () => ErrorGuard.Run("refresh the details window", () => _detailsWindow?.Render(state)));

        _monitor.Start();

        if (_settings.CheckForUpdates)
        {
            ErrorGuard.Forget(
                "update check at startup", () => CheckForUpdatesAsync(announceUpToDate: false));
        }

        // Only once the native libraries are loaded can the application spot its
        // own extraction folder - before that it either cleaned up nothing or
        // removed itself.
        ErrorGuard.Forget("clean up temporary files", async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(15)).ConfigureAwait(false);
            TempCleanup.RemoveStaleExtractions();
        });

        ErrorGuard.Run("show the changes", ShowReleaseNotesAfterUpdate);

        if (SelfInstaller.ShouldOffer && !_settings.InstallPromptShown)
        {
            ErrorGuard.Run("offer the setup", ShowInstallPrompt);
        }
    }

    /// <summary>
    /// Shows once after an update what has changed since, and then records the
    /// running version.
    /// </summary>
    /// <remarks>
    /// On the very first start the summary is skipped: someone just getting to
    /// know the application has no interest in its prehistory. The version is
    /// recorded regardless, so that the next update has something to compare to.
    /// </remarks>
    private void ShowReleaseNotesAfterUpdate()
    {
        var current = CurrentVersion;
        var previous = ReleaseHistory.Parse(_settings.LastRunVersion);

        // Whether the application has ever run is betrayed only by the settings
        // file. At this point nothing has created it yet.
        var isFirstInstall = !File.Exists(AppPaths.SettingsFile);

        if (ReleaseHistory.ShouldShow(previous, current, isFirstInstall))
        {
            // Without a recorded version no span can be formed - then it stays
            // with the entry for the running version.
            var changes = previous is null
                ? ChangelogResource.Only(current.Number)
                : ChangelogResource.Between(previous, current);

            if (changes.Count > 0)
            {
                ShowReleaseNotes(changes, previous);
            }
        }

        var recorded = ReleaseHistory.Format(current);
        if (_settings.LastRunVersion != recorded)
        {
            _settings.LastRunVersion = recorded;
            _settingsStore.Save(_settings);
        }
    }

    /// <summary>Shows the summary of changes after an update.</summary>
    /// <remarks>
    /// <c>Activate</c> alone does not bring the window forward. After an update
    /// the application restarts itself, and Windows refuses the foreground to a
    /// process that did not have it - the window then opens behind whatever the
    /// user happens to be doing, and the summary goes unread. Briefly declaring
    /// it topmost puts it in front; the flag is dropped again at once, so that
    /// the window does not sit above everything else afterwards.
    /// </remarks>
    private static void ShowReleaseNotes(IReadOnlyList<ReleaseNotes> releases, ProgramVersion? previous)
    {
        var window = new ReleaseNotesWindow();
        window.Render(releases, previous, ChangelogResource.IsTranslated);

        window.Topmost = true;
        window.Show();
        window.Activate();
        window.Topmost = false;
    }

    /// <summary>
    /// Relabels the parts of the interface that outlive a language change.
    /// </summary>
    /// <remarks>
    /// Most windows are created afresh every time they open and pick up their
    /// texts then anyway. Two things persist, though: the tray context menu and
    /// the details window once created. Without this call those two would stay
    /// in the old language - until the next start, which would look like half a
    /// language change.
    /// </remarks>
    private void ApplyLanguage()
    {
        _tray?.ApplyTexts();

        if (_detailsWindow is { } window)
        {
            window.ApplyTexts();

            if (_monitor is not null)
            {
                window.Render(_monitor.State);
            }
        }
    }

    private void ShowAbout()
    {
        var window = new AboutWindow(RepositoryUri, CurrentVersion);
        window.RepositoryRequested += (_, address) =>
            ErrorGuard.Run("open the project page", () => OpenReleasePage(address));
        window.ReleaseNotesRequested += (_, _) => ErrorGuard.Run(
            "show the changelog", () => ShowReleaseNotes(ChangelogResource.All(), previous: null));

        window.Show();
        window.Activate();
    }

    /// <summary>
    /// Offers once to set the application up permanently. The answer is recorded
    /// so that the question does not return on every start.
    /// </summary>
    private void ShowInstallPrompt()
    {
        var window = new InstallPromptWindow();

        // The language choice applies regardless of the answer. Someone who
        // changes it and then declines still wanted the change - which is why
        // both buttons record it.
        window.LanguageChanged += (_, _) => ErrorGuard.Run("apply the language", ApplyLanguage);

        window.Declined += (_, _) => ErrorGuard.Run("record the answer", () =>
            RememberAnswer(installed: false, window.SelectedLanguage));

        window.Installed += (_, _) => ErrorGuard.Run("finish the setup", () =>
        {
            RememberAnswer(installed: true, window.SelectedLanguage);
            // The installed copy is already waiting for this one to end.
            RequestShutdown();
        });

        window.Show();
        window.Activate();
    }

    /// <summary>
    /// Moves the application to its permanent location and then ends this
    /// instance. Called from the settings when autostart is switched on there:
    /// an entry pointing at the downloads folder would not survive the first
    /// clean-up of that folder.
    /// </summary>
    private InstallResult InstallPermanently()
    {
        var result = SelfInstaller.Install();

        if (result.Succeeded)
        {
            RememberAnswer(installed: true);

            // Return first, so that the settings window can still show the
            // message - the new instance is waiting for this one to end anyway.
            Dispatcher.UIThread.Post(() => ErrorGuard.Run("exit after the setup", RequestShutdown));
        }

        return result;
    }

    /// <param name="language">
    /// The language chosen in the window. Recorded along with the answer, so
    /// that the installed copy finds it on its next start - without that it
    /// would fall back to the language of the system there.
    /// </param>
    private void RememberAnswer(bool installed, Language? language = null)
    {
        _settings.InstallPromptShown = true;

        if (installed)
        {
            _settings.LaunchAtLogin = true;
        }

        if (language is not null)
        {
            _settings.Language = language.Code;
        }

        _settingsStore.Save(_settings);
    }

    /// <summary>
    /// Order of the token sources: the application's own sign-in first, then a
    /// manually stored token, the environment variable, and finally the
    /// credentials of the Claude Code CLI. If the API rejects a source, the call
    /// moves on to the next.
    /// </summary>
    private List<ITokenProvider> BuildTokenProviders() =>
    [
        _oauthTokenProvider!,
        new SecretStoreTokenProvider(_secretStore),
        new EnvironmentTokenProvider(),
        new ClaudeCliTokenProvider()
    ];

    private static ISecretStore CreateSecretStore() =>
        SecretStoreFactory.CreateForCurrentPlatform();

    /// <summary>Identity of the public repository releases are taken from.</summary>
    private const string RepositoryOwner = "sven-reichelt";
    private const string RepositoryName = "Claude-UsageChecker";

    /// <summary>Address of the project page, as the about window offers it.</summary>
    public static Uri RepositoryUri { get; } =
        new($"https://github.com/{RepositoryOwner}/{RepositoryName}");

    /// <remarks>
    /// The channel is read on every check rather than captured once: whoever
    /// switches to pre-releases in the settings expects the next check to follow
    /// it, not the next start.
    /// </remarks>
    private IUpdateService CreateUpdateService(HttpClient httpClient) =>
        new GitHubReleaseUpdateService(
            httpClient, RepositoryOwner, RepositoryName, CurrentVersion, () => _settings.Channel);

    private void ShowDetails()
    {
        if (_monitor is null)
        {
            return;
        }

        _detailsWindow ??= CreateDetailsWindow();
        _detailsWindow.Render(_monitor.State);
        _detailsWindow.Show();
        _detailsWindow.Activate();
    }

    private DetailsWindow CreateDetailsWindow()
    {
        var window = new DetailsWindow();
        window.RefreshRequested += (_, _) => ErrorGuard.Forget("trigger a call", RefreshAndCheckAsync);
        window.ReleasePageRequested += (_, page) =>
            ErrorGuard.Run("open the release page", () => OpenReleasePage(page));
        window.InstallRequested += (_, _) =>
            ErrorGuard.Forget("install the update", InstallUpdateAsync);
        window.Closing += (_, e) =>
        {
            // The window is only hidden - the application carries on.
            e.Cancel = true;
            window.Hide();
        };
        return window;
    }

    private void ShowSettings()
    {
        var window = new SettingsWindow(
            _settingsStore,
            _settings,
            _oauthTokenStore,
            InstallPermanently);

        window.SettingsChanged += (_, settings) =>
        {
            _settings = settings;
            ErrorGuard.Run("apply the language", ApplyLanguage);
            ErrorGuard.Forget("call after a settings change", RefreshAsync);
        };
        window.SignInRequested += (_, _) => ErrorGuard.Run("open the sign-in", () => ShowSignIn(window));

        window.Show();
        window.Activate();
    }

    /// <summary>
    /// Points out that the application's own sign-in has expired. The window is
    /// not forced on anyone - the notice waits there until it is opened.
    /// </summary>
    private void ShowSignInExpired(string reason)
    {
        _detailsWindow ??= CreateDetailsWindow();
        _detailsWindow.SetSignInNotice(T.DetailsSignInExpired(reason));
    }

    private void ShowSignIn(SettingsWindow owner)
    {
        var window = new SignInWindow(_oauthClient, _oauthTokenStore);
        window.SignedIn += (_, _) =>
        {
            owner.RefreshSignInStatus();
            _detailsWindow?.SetSignInNotice(null);
            ErrorGuard.Forget("call after signing in", RefreshAsync);
        };
        window.Show();
        window.Activate();
    }

    private async Task RefreshAsync()
    {
        if (_monitor is not null)
        {
            await _monitor.RefreshNowAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// The refresh button of the details window: the figures first, and where
    /// the setting allows it, a look for a new version afterwards.
    /// </summary>
    /// <remarks>
    /// In that order deliberately. The figures are what the button is for and
    /// what the user is waiting for; the version is the errand it runs on the
    /// way. A failed update check must not hold the figures back either - hence
    /// its own guard rather than one try around both.
    /// </remarks>
    private async Task RefreshAndCheckAsync()
    {
        await RefreshAsync().ConfigureAwait(false);

        if (_settings.RefreshChecksForUpdates)
        {
            await CheckForUpdatesAsync(announceUpToDate: true).ConfigureAwait(false);
        }
    }

    private async Task CheckForUpdatesAsync(bool announceUpToDate)
    {
        if (_updateService is null)
        {
            return;
        }

        var result = await _updateService.CheckAsync().ConfigureAwait(false);

        // The silent check at startup speaks up only when there is something to say.
        var isNoteworthy = result.Status == UpdateCheckStatus.UpdateAvailable;
        if (!isNoteworthy && !announceUpToDate)
        {
            return;
        }

        await Dispatcher.UIThread.InvokeAsync(() => ShowUpdateResult(result, openWindow: announceUpToDate || isNoteworthy));
    }

    /// <summary>
    /// Shows the result in the details window. Nothing is downloaded or executed
    /// on purpose - installing stays a deliberate act of the user.
    /// </summary>
    private void ShowUpdateResult(UpdateCheckResult result, bool openWindow)
    {
        _detailsWindow ??= CreateDetailsWindow();

        // Installing is only offered where it can succeed: it needs the file,
        // the checksum, and a build that is allowed to replace itself. In a
        // development build dozens of files sit side by side - swapping the exe
        // would be pointless there.
        _pendingUpdate = result;
        _detailsWindow.SetUpdateNotice(
            result.Message,
            result.ReleasePage,
            canInstall: result.CanInstall && UpdateInstaller.IsSupported);

        if (!openWindow)
        {
            return;
        }

        if (_monitor is not null)
        {
            _detailsWindow.Render(_monitor.State);
        }

        _detailsWindow.Show();
        _detailsWindow.Activate();
    }

    private static void OpenReleasePage(Uri page) =>
        Process.Start(new ProcessStartInfo(page.ToString()) { UseShellExecute = true });

    /// <summary>
    /// Installs the new version and then ends this instance - the new one is
    /// already running by then and only waits for this one to finish.
    /// </summary>
    private async Task InstallUpdateAsync()
    {
        if (_pendingUpdate is not { } update || _detailsWindow is null || _updateHttpClient is null)
        {
            return;
        }

        _detailsWindow.SetInstallProgress(T.UpdateDownloading, busy: true);

        var installer = new UpdateInstaller(_updateHttpClient);
        var result = await installer.InstallAsync(update).ConfigureAwait(true);

        if (!result.Succeeded)
        {
            _detailsWindow.SetInstallProgress(result.Message, busy: false);
            return;
        }

        _detailsWindow.SetInstallProgress(result.Message, busy: true);

        // The new instance is waiting for this one to end - so do not dawdle.
        RequestShutdown();
    }

    private void RequestShutdown()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }

    private void Shutdown()
    {
        _tray?.Dispose();
        _monitor?.DisposeAsync().AsTask().GetAwaiter().GetResult();
        _oauthTokenProvider?.Dispose();
        _usageHttpClient?.Dispose();
        _updateHttpClient?.Dispose();
        _oauthHttpClient?.Dispose();

        _tray = null;
        _monitor = null;
        _oauthTokenProvider = null;
        _usageHttpClient = null;
        _updateHttpClient = null;
        _oauthHttpClient = null;
    }

    /// <summary>Releases every resource. Called when the application shuts down.</summary>
    public void Dispose()
    {
        Shutdown();
        GC.SuppressFinalize(this);
    }

    /// <summary>Current program version, as set at build time.</summary>
    public static ProgramVersion CurrentVersion => ProgramVersion.Current;
}
