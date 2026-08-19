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
    private UsageMonitor? _monitor;
    private TrayIconController? _tray;
    private DetailsWindow? _detailsWindow;
    private IUpdateService? _updateService;

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

        var tokenProvider = new ChainedTokenProvider(BuildTokenProviders());
        var apiClient = new AnthropicUsageApiClient(_usageHttpClient, tokenProvider, options);

        _monitor = new UsageMonitor(apiClient, new MonitorOptions
        {
            PollInterval = _settings.PollInterval,
            WarningThreshold = _settings.WarningThreshold,
            CriticalThreshold = _settings.CriticalThreshold
        });

        _updateHttpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        _updateService = CreateUpdateService(_updateHttpClient);

        _tray = new TrayIconController(_monitor, () => _settings);
        _tray.ShowDetails += (_, _) => ShowDetails();
        _tray.ShowSettings += (_, _) => ShowSettings();
        _tray.RefreshRequested += (_, _) => _ = RefreshAsync();
        _tray.CheckForUpdatesRequested += (_, _) => _ = CheckForUpdatesAsync(announceUpToDate: true);
        _tray.ExitRequested += (_, _) => RequestShutdown();

        _monitor.StateChanged += (_, state) =>
            Dispatcher.UIThread.Post(() => _detailsWindow?.Render(state));

        _monitor.Start();

        if (_settings.CheckForUpdates)
        {
            _ = CheckForUpdatesAsync(announceUpToDate: false);
        }
    }

    /// <summary>
    /// Reihenfolge der Tokenquellen: eigenes Langzeit-Token zuerst, danach die
    /// Umgebungsvariable, zuletzt die Anmeldedaten der Claude-Code-CLI.
    /// </summary>
    private List<ITokenProvider> BuildTokenProviders() =>
    [
        new SecretStoreTokenProvider(_secretStore),
        new EnvironmentTokenProvider(),
        new ClaudeCliTokenProvider()
    ];

    private static ISecretStore CreateSecretStore() =>
        SecretStoreFactory.CreateForCurrentPlatform();

    private static IUpdateService CreateUpdateService(HttpClient httpClient)
    {
        // Solange die Veroeffentlichungen nicht oeffentlich sind, bleibt die
        // Pruefung deaktiviert. Fuer ein oeffentliches Repository genuegt es,
        // hier den GitHubReleaseUpdateService zurueckzugeben.
        _ = httpClient;
        return new DisabledUpdateService();
    }

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
        window.RefreshRequested += (_, _) => _ = RefreshAsync();
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
        var window = new SettingsWindow(_secretStore, _settingsStore, _settings);
        window.SettingsChanged += (_, settings) =>
        {
            _settings = settings;
            _ = RefreshAsync();
        };
        window.Show();
        window.Activate();
    }

    private async Task RefreshAsync()
    {
        if (_monitor is null)
        {
            return;
        }

        try
        {
            await _monitor.RefreshNowAsync().ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Beim Beenden erwartet.
        }
    }

    private async Task CheckForUpdatesAsync(bool announceUpToDate)
    {
        if (_updateService is null)
        {
            return;
        }

        var result = await _updateService.CheckAsync().ConfigureAwait(false);

        if (result.Status == UpdateCheckStatus.UpToDate && !announceUpToDate)
        {
            return;
        }

        await Dispatcher.UIThread.InvokeAsync(() => ShowUpdateResult(result));
    }

    private static void ShowUpdateResult(UpdateCheckResult result)
    {
        if (result is { Status: UpdateCheckStatus.UpdateAvailable, ReleasePage: { } page })
        {
            // Bewusst nur oeffnen, nicht selbst herunterladen und ausfuehren.
            Process.Start(new ProcessStartInfo(page.ToString()) { UseShellExecute = true });
        }
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
        _usageHttpClient?.Dispose();
        _updateHttpClient?.Dispose();

        _tray = null;
        _monitor = null;
        _usageHttpClient = null;
        _updateHttpClient = null;
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
