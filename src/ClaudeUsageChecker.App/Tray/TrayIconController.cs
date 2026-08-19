using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Threading;
using ClaudeUsageChecker.App.Settings;
using ClaudeUsageChecker.Core.Formatting;
using ClaudeUsageChecker.Core.Services;

namespace ClaudeUsageChecker.App.Tray;

/// <summary>
/// Verbindet den <see cref="UsageMonitor"/> mit dem Symbol im Infobereich:
/// haelt Tooltip, Symbolfarbe und Kontextmenue aktuell.
/// </summary>
public sealed class TrayIconController : IDisposable
{
    private readonly UsageMonitor _monitor;
    private readonly Func<AppSettings> _settings;
    private readonly TimeProvider _timeProvider;

    private readonly TrayIcon _trayIcon;
    private readonly NativeMenuItem _statusItem;
    private readonly DispatcherTimer _tooltipTimer;

    public TrayIconController(
        UsageMonitor monitor,
        Func<AppSettings> settings,
        TimeProvider? timeProvider = null)
    {
        _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _timeProvider = timeProvider ?? TimeProvider.System;

        _statusItem = new NativeMenuItem("Daten werden geladen ...") { IsEnabled = false };

        _trayIcon = new TrayIcon
        {
            Icon = LoadIcon(TrayIconSeverity.Inactive),
            ToolTipText = "Claude UsageChecker",
            IsVisible = true,
            Menu = BuildMenu()
        };

        _trayIcon.Clicked += (_, _) => ShowDetails?.Invoke(this, EventArgs.Empty);
        _monitor.StateChanged += OnStateChanged;

        // Die Restzeit laeuft weiter, auch wenn kein neuer Abruf stattfindet.
        _tooltipTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromSeconds(30)
        };
        _tooltipTimer.Tick += (_, _) => Render(_monitor.State);
        _tooltipTimer.Start();

        Render(_monitor.State);
    }

    /// <summary>Der Nutzer moechte die Detailansicht sehen.</summary>
    public event EventHandler? ShowDetails;

    /// <summary>Der Nutzer moechte die Einstellungen oeffnen.</summary>
    public event EventHandler? ShowSettings;

    /// <summary>Der Nutzer hat einen sofortigen Abruf angefordert.</summary>
    public event EventHandler? RefreshRequested;

    /// <summary>Der Nutzer moechte auf Aktualisierungen pruefen.</summary>
    public event EventHandler? CheckForUpdatesRequested;

    /// <summary>Der Nutzer moechte die Anwendung beenden.</summary>
    public event EventHandler? ExitRequested;

    private NativeMenu BuildMenu()
    {
        var menu = new NativeMenu();
        menu.Add(_statusItem);
        menu.Add(new NativeMenuItemSeparator());

        var details = new NativeMenuItem("Details anzeigen");
        details.Click += (_, _) => ShowDetails?.Invoke(this, EventArgs.Empty);
        menu.Add(details);

        var refresh = new NativeMenuItem("Jetzt aktualisieren");
        refresh.Click += (_, _) => RefreshRequested?.Invoke(this, EventArgs.Empty);
        menu.Add(refresh);

        menu.Add(new NativeMenuItemSeparator());

        var settings = new NativeMenuItem("Einstellungen ...");
        settings.Click += (_, _) => ShowSettings?.Invoke(this, EventArgs.Empty);
        menu.Add(settings);

        var update = new NativeMenuItem("Auf Aktualisierungen pruefen ...");
        update.Click += (_, _) => CheckForUpdatesRequested?.Invoke(this, EventArgs.Empty);
        menu.Add(update);

        menu.Add(new NativeMenuItemSeparator());

        var exit = new NativeMenuItem("Beenden");
        exit.Click += (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty);
        menu.Add(exit);

        return menu;
    }

    private void OnStateChanged(object? sender, UsageState state) =>
        Dispatcher.UIThread.Post(() => Render(state));

    private void Render(UsageState state)
    {
        var now = _timeProvider.GetLocalNow();
        var settings = _settings();

        _trayIcon.ToolTipText = UsageFormatter.ToTooltip(state, now);

        var severity = TrayIconSeverityResolver.Resolve(
            state, settings.WarningThreshold, settings.CriticalThreshold);
        _trayIcon.Icon = LoadIcon(severity);

        _statusItem.Header = state.Snapshot?.Session is { } session
            ? UsageFormatter.ToDetailLine("Sitzung", session, now)
            : state.Message ?? "Keine Daten";
    }

    private static WindowIcon LoadIcon(TrayIconSeverity severity)
    {
        var name = severity switch
        {
            TrayIconSeverity.Warning => "tray-warning",
            TrayIconSeverity.Critical => "tray-critical",
            TrayIconSeverity.Inactive => "tray-inactive",
            _ => "tray-normal"
        };

        var uri = new Uri($"avares://ClaudeUsageChecker/Assets/{name}.png");
        return new WindowIcon(AssetLoader.Open(uri));
    }

    public void Dispose()
    {
        _tooltipTimer.Stop();
        _monitor.StateChanged -= OnStateChanged;
        _trayIcon.IsVisible = false;
        _trayIcon.Dispose();
    }
}
