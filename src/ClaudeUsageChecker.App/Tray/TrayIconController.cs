using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Threading;
using ClaudeUsageChecker.App.Settings;
using ClaudeUsageChecker.Core.Formatting;
using ClaudeUsageChecker.Core.Models;
using ClaudeUsageChecker.Core.Services;

namespace ClaudeUsageChecker.App.Tray;

/// <summary>
/// Verbindet den <see cref="UsageMonitor"/> mit dem Symbol im Infobereich:
/// haelt Tooltip, Symbolfarbe und Kontextmenue aktuell.
/// </summary>
public sealed class TrayIconController : IDisposable
{
    /// <summary>
    /// So viele Statuszeilen haelt das Menue bereit: Sitzung, Woche gesamt,
    /// Woche Opus, Woche Sonnet und das Zusatzkontingent.
    /// </summary>
    private const int StatusSlotCount = 5;

    private readonly UsageMonitor _monitor;
    private readonly Func<AppSettings> _settings;
    private readonly TimeProvider _timeProvider;

    private readonly TrayIcon _trayIcon;
    private readonly List<NativeMenuItem> _statusItems = [];
    private readonly DispatcherTimer _tooltipTimer;

    public TrayIconController(
        UsageMonitor monitor,
        Func<AppSettings> settings,
        TimeProvider? timeProvider = null)
    {
        _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _timeProvider = timeProvider ?? TimeProvider.System;

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

    /// <summary>
    /// Der Nutzer hat auf das Symbol geklickt und moechte die Detailansicht sehen.
    /// Bewusst der einzige Weg dorthin - siehe Aufbau des Menues.
    /// </summary>
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

        // Die Statuszeilen werden einmal angelegt und danach nur noch beschriftet.
        // Ein Umbauen des Menues zur Laufzeit waere heikel, solange es geoeffnet ist.
        for (var i = 0; i < StatusSlotCount; i++)
        {
            var item = new NativeMenuItem { IsEnabled = false, IsVisible = false };
            _statusItems.Add(item);
            menu.Add(item);
        }

        menu.Add(new NativeMenuItemSeparator());

        // Kein "Details anzeigen": Der Linksklick auf das Symbol oeffnet die
        // Detailansicht, und die Zahlen stehen bereits in den Statuszeilen
        // darueber. Ein Eintrag, der nur denselben Weg noch einmal anbietet,
        // macht das Menue laenger ohne etwas hinzuzufuegen.
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

        RenderStatusItems(state, now);
    }

    private void RenderStatusItems(UsageState state, DateTimeOffset now)
    {
        var lines = BuildStatusLines(state, now);

        for (var i = 0; i < _statusItems.Count; i++)
        {
            if (i < lines.Count)
            {
                _statusItems[i].Header = lines[i];
                _statusItems[i].IsVisible = true;
            }
            else
            {
                _statusItems[i].IsVisible = false;
            }
        }
    }

    /// <summary>
    /// Baut die Statuszeilen des Menues. Nicht gemeldete Fenster entfallen -
    /// je nach Abonnement liefert die API etwa kein Opus-Wochenlimit.
    /// </summary>
    private static List<string> BuildStatusLines(UsageState state, DateTimeOffset now)
    {
        if (state.Snapshot is not { } snapshot)
        {
            return [state.Message ?? "Keine Daten"];
        }

        var lines = new List<string>(StatusSlotCount);

        foreach (var (label, window) in UsageFormatter.EnumerateWindows(snapshot))
        {
            lines.Add(UsageFormatter.ToMenuLine(label, window, now));
        }

        if (UsageFormatter.ToExtraUsageLine(snapshot.ExtraUsage) is { } extra)
        {
            lines.Add(extra);
        }

        return lines.Count == 0 ? ["Keine Limits gemeldet"] : lines;
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
