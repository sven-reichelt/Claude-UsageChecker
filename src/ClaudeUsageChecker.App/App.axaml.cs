using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using ClaudeUsageChecker.Core.Platform;
using ClaudeUsageChecker.Core.Services;

namespace ClaudeUsageChecker.App;

/// <summary>
/// Einstiegspunkt und Composition Root. Die Anwendung besitzt kein Hauptfenster;
/// sie lebt ausschliesslich im Infobereich.
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
            // Ohne Hauptfenster muss das Beenden ausdruecklich erfolgen.
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            desktop.Exit += (_, _) => Shutdown();

            _settings = _settingsStore.Load();
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

        // Die eigene Anmeldung bekommt einen eigenen HttpClient: Sie spricht mit
        // console.anthropic.com, nicht mit der Basisadresse des Nutzungsabrufs.
        _oauthHttpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        _oauthClient = new AnthropicOAuthClient(_oauthHttpClient);
        _oauthTokenStore = new OAuthTokenStore(_secretStore);
        _oauthTokenProvider = new OAuthTokenProvider(_oauthTokenStore, _oauthClient);

        // Laeuft die eigene Anmeldung ab, faellt der Abruf zwar auf Claude Code
        // zurueck - der Nutzer soll das aber erfahren, statt die Unabhaengigkeit
        // unbemerkt zu verlieren.
        _oauthTokenProvider.SignInExpired += (_, grund) => Dispatcher.UIThread.Post(
            () => ErrorGuard.Run("Hinweis auf abgelaufene Anmeldung", () => ShowSignInExpired(grund)));

        var apiClient = new AnthropicUsageApiClient(_usageHttpClient, BuildTokenProviders(), options);

        _monitor = new UsageMonitor(apiClient, new MonitorOptions
        {
            PollInterval = _settings.PollInterval,
            WarningThreshold = _settings.WarningThreshold,
            CriticalThreshold = _settings.CriticalThreshold
        });

        _updateHttpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        _updateService = CreateUpdateService(_updateHttpClient);

        // Jede Aktion aus dem Infobereich laeuft durch den Wachposten: Ein Fehler
        // darin darf die Anwendung nicht kommentarlos beenden.
        _tray = new TrayIconController(_monitor, () => _settings);
        _tray.ShowDetails += (_, _) => ErrorGuard.Run("Details anzeigen", ShowDetails);
        _tray.ShowSettings += (_, _) => ErrorGuard.Run("Einstellungen oeffnen", ShowSettings);
        _tray.RefreshRequested += (_, _) => ErrorGuard.Forget("Abruf anstossen", RefreshAsync);
        _tray.CheckForUpdatesRequested += (_, _) => ErrorGuard.Forget(
            "Aktualisierungspruefung", () => CheckForUpdatesAsync(announceUpToDate: true));
        _tray.ExitRequested += (_, _) => ErrorGuard.Run("Beenden", RequestShutdown);

        _monitor.StateChanged += (_, state) => Dispatcher.UIThread.Post(
            () => ErrorGuard.Run("Detailansicht aktualisieren", () => _detailsWindow?.Render(state)));

        _monitor.Start();

        if (_settings.CheckForUpdates)
        {
            ErrorGuard.Forget(
                "Aktualisierungspruefung beim Start", () => CheckForUpdatesAsync(announceUpToDate: false));
        }
    }

    /// <summary>
    /// Reihenfolge der Tokenquellen: die eigene Anmeldung zuerst, danach ein von
    /// Hand hinterlegtes Token, die Umgebungsvariable und zuletzt die
    /// Anmeldedaten der Claude-Code-CLI. Lehnt die API eine Quelle ab, rueckt
    /// der Abruf zur naechsten vor.
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

    /// <summary>Kennung des oeffentlichen Repositorys, aus dem Veroeffentlichungen bezogen werden.</summary>
    private const string RepositoryOwner = "sven-reichelt";
    private const string RepositoryName = "Claude-UsageChecker";

    private static IUpdateService CreateUpdateService(HttpClient httpClient) =>
        new GitHubReleaseUpdateService(httpClient, RepositoryOwner, RepositoryName, CurrentVersion);

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
        window.RefreshRequested += (_, _) => ErrorGuard.Forget("Abruf anstossen", RefreshAsync);
        window.ReleasePageRequested += (_, page) =>
            ErrorGuard.Run("Release-Seite oeffnen", () => OpenReleasePage(page));
        window.InstallRequested += (_, _) =>
            ErrorGuard.Forget("Aktualisierung einspielen", InstallUpdateAsync);
        window.Closing += (_, e) =>
        {
            // Das Fenster wird nur versteckt - die Anwendung laeuft weiter.
            e.Cancel = true;
            window.Hide();
        };
        return window;
    }

    private void ShowSettings()
    {
        var validator = new TokenValidator(_usageHttpClient!);
        var window = new SettingsWindow(
            _secretStore,
            _settingsStore,
            _settings,
            token => validator.ValidateAsync(token),
            _oauthTokenStore);

        window.SettingsChanged += (_, settings) =>
        {
            _settings = settings;
            ErrorGuard.Forget("Abruf nach Einstellungsaenderung", RefreshAsync);
        };
        window.SignInRequested += (_, _) => ErrorGuard.Run("Anmeldung oeffnen", () => ShowSignIn(window));

        window.Show();
        window.Activate();
    }

    /// <summary>
    /// Weist auf eine abgelaufene eigene Anmeldung hin. Das Fenster wird dabei
    /// nicht aufgedraengt - der Hinweis steht bereit, sobald es geoeffnet wird.
    /// </summary>
    private void ShowSignInExpired(string grund)
    {
        _detailsWindow ??= CreateDetailsWindow();
        _detailsWindow.SetSignInNotice(
            "Die eigene Anmeldung ist abgelaufen und wurde entfernt. "
            + "Bitte in den Einstellungen neu anmelden. Bis dahin wird - sofern vorhanden - "
            + "das Token von Claude Code mitgelesen. Grund: " + grund);
    }

    private void ShowSignIn(SettingsWindow owner)
    {
        var window = new SignInWindow(_oauthClient, _oauthTokenStore);
        window.SignedIn += (_, _) =>
        {
            owner.RefreshSignInStatus();
            _detailsWindow?.SetSignInNotice(null);
            ErrorGuard.Forget("Abruf nach Anmeldung", RefreshAsync);
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

    private async Task CheckForUpdatesAsync(bool announceUpToDate)
    {
        if (_updateService is null)
        {
            return;
        }

        var result = await _updateService.CheckAsync().ConfigureAwait(false);

        // Die stille Pruefung beim Start meldet sich nur, wenn es etwas zu melden gibt.
        var isNoteworthy = result.Status == UpdateCheckStatus.UpdateAvailable;
        if (!isNoteworthy && !announceUpToDate)
        {
            return;
        }

        await Dispatcher.UIThread.InvokeAsync(() => ShowUpdateResult(result, openWindow: announceUpToDate || isNoteworthy));
    }

    /// <summary>
    /// Zeigt das Ergebnis in der Detailansicht. Es wird bewusst nichts
    /// heruntergeladen oder ausgefuehrt - das Einspielen bleibt eine bewusste
    /// Handlung des Nutzers.
    /// </summary>
    private void ShowUpdateResult(UpdateCheckResult result, bool openWindow)
    {
        _detailsWindow ??= CreateDetailsWindow();

        // Das Einspielen wird nur angeboten, wenn es auch gelingen kann: Es
        // braucht Datei und Pruefsumme und eine Fassung, die sich selbst
        // ersetzen darf. Im Entwicklungsstand liegen Dutzende Dateien
        // nebeneinander - da waere ein Tausch der Exe sinnlos.
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
    /// Spielt die neue Fassung ein und beendet danach diese Instanz - die neue
    /// laeuft dann bereits und wartet nur noch auf das Ende dieser hier.
    /// </summary>
    private async Task InstallUpdateAsync()
    {
        if (_pendingUpdate is not { } update || _detailsWindow is null || _updateHttpClient is null)
        {
            return;
        }

        _detailsWindow.SetInstallProgress("Die neue Fassung wird geladen und geprueft ...", busy: true);

        var installer = new UpdateInstaller(_updateHttpClient);
        var ergebnis = await installer.InstallAsync(update).ConfigureAwait(true);

        if (!ergebnis.Succeeded)
        {
            _detailsWindow.SetInstallProgress(ergebnis.Message, busy: false);
            return;
        }

        _detailsWindow.SetInstallProgress(ergebnis.Message, busy: true);

        // Die neue Instanz wartet auf das Ende dieser - also nicht troedeln.
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

    /// <summary>Gibt alle Ressourcen frei. Wird beim Beenden der Anwendung aufgerufen.</summary>
    public void Dispose()
    {
        Shutdown();
        GC.SuppressFinalize(this);
    }

    /// <summary>Aktuelle Programmversion, wie sie beim Bauen gesetzt wurde.</summary>
    public static Version CurrentVersion { get; } =
        Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0);
}
