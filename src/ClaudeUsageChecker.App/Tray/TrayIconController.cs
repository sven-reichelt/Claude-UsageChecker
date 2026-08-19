using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Threading;
using ClaudeUsageChecker.App.Settings;
using ClaudeUsageChecker.Core.Formatting;
using ClaudeUsageChecker.Core.Localization;
using ClaudeUsageChecker.Core.Models;
using ClaudeUsageChecker.Core.Services;

namespace ClaudeUsageChecker.App.Tray;

/// <summary>
/// Connects the <see cref="UsageMonitor"/> to the tray icon: keeps tooltip,
/// icon colour and context menu up to date.
/// </summary>
public sealed class TrayIconController : IDisposable
{
    /// <summary>
    /// This many status lines the menu keeps ready: session, weekly total, the
    /// extra usage, and room for up to five model-specific weekly limits.
    /// </summary>
    /// <remarks>
    /// The slots are created once and only relabelled afterwards - rebuilding
    /// the menu at runtime would be delicate while it is open. How many
    /// model-specific limits the API reports is up to the API; today it is one
    /// (Fable), earlier two were foreseen. The supply is therefore generous.
    /// Surplus lines would otherwise fall away in silence -
    /// <c>ThereAreEnoughSlotsForEveryReportedLimit</c> watches over that.
    /// </remarks>
    internal const int StatusSlotCount = 8;

    private readonly UsageMonitor _monitor;
    private readonly Func<AppSettings> _settings;
    private readonly TimeProvider _timeProvider;

    private readonly TrayIcon _trayIcon;
    private readonly List<NativeMenuItem> _statusItems = [];
    private readonly DispatcherTimer _tooltipTimer;

    // The command entries are kept so that a language change can relabel them.
    // The menu itself is not rebuilt in the process - that would be delicate
    // while it is open.
    private readonly List<(NativeMenuItem Item, Func<string> Text)> _commandItems = [];

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
            ToolTipText = T.AppName,
            IsVisible = true,
            Menu = BuildMenu()
        };

        _trayIcon.Clicked += (_, _) => ShowDetails?.Invoke(this, EventArgs.Empty);
        _monitor.StateChanged += OnStateChanged;

        // The remaining time keeps running even when no new call is made.
        _tooltipTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromSeconds(30)
        };
        _tooltipTimer.Tick += (_, _) => Render(_monitor.State);
        _tooltipTimer.Start();

        Render(_monitor.State);
    }

    /// <summary>
    /// The user clicked the icon and wants to see the details window.
    /// Deliberately the only route there - see how the menu is built.
    /// </summary>
    public event EventHandler? ShowDetails;

    /// <summary>The user wants to open the settings.</summary>
    public event EventHandler? ShowSettings;

    /// <summary>The user asked for an immediate call.</summary>
    public event EventHandler? RefreshRequested;

    /// <summary>The user wants to check for updates.</summary>
    public event EventHandler? CheckForUpdatesRequested;

    /// <summary>The user wants to know what the application is and which version runs.</summary>
    public event EventHandler? ShowAboutRequested;

    /// <summary>The user wants to exit the application.</summary>
    public event EventHandler? ExitRequested;

    private NativeMenu BuildMenu()
    {
        var menu = new NativeMenu();

        // The status lines are created once and only relabelled afterwards.
        // Rebuilding the menu at runtime would be delicate while it is open.
        for (var i = 0; i < StatusSlotCount; i++)
        {
            var item = new NativeMenuItem { IsEnabled = false, IsVisible = false };
            _statusItems.Add(item);
            menu.Add(item);
        }

        menu.Add(new NativeMenuItemSeparator());

        // No "show details": a left click on the icon opens the details window,
        // and the figures are already in the status lines above. An entry that
        // merely offers the same route again makes the menu longer without
        // adding anything.
        var refresh = CommandItem(() => T.TrayRefreshNow);
        refresh.Click += (_, _) => RefreshRequested?.Invoke(this, EventArgs.Empty);
        menu.Add(refresh);

        menu.Add(new NativeMenuItemSeparator());

        var settings = CommandItem(() => T.TraySettings);
        settings.Click += (_, _) => ShowSettings?.Invoke(this, EventArgs.Empty);
        menu.Add(settings);

        var update = CommandItem(() => T.TrayCheckForUpdates);
        update.Click += (_, _) => CheckForUpdatesRequested?.Invoke(this, EventArgs.Empty);
        menu.Add(update);

        var about = CommandItem(() => T.TrayAbout);
        about.Click += (_, _) => ShowAboutRequested?.Invoke(this, EventArgs.Empty);
        menu.Add(about);

        menu.Add(new NativeMenuItemSeparator());

        var exit = CommandItem(() => T.TrayExit);
        exit.Click += (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty);
        menu.Add(exit);

        return menu;
    }

    /// <summary>
    /// Creates a menu entry and remembers where its text comes from.
    /// </summary>
    private NativeMenuItem CommandItem(Func<string> text)
    {
        var item = new NativeMenuItem(text());
        _commandItems.Add((item, text));
        return item;
    }

    /// <summary>
    /// Relabels menu and tooltip - after a language change.
    /// </summary>
    public void ApplyTexts()
    {
        foreach (var (item, text) in _commandItems)
        {
            item.Header = text();
        }

        Render(_monitor.State);
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
    /// Builds the status lines of the menu. Windows that are not reported are
    /// left out - depending on the subscription the API may report no
    /// model-specific weekly limit at all.
    /// </summary>
    internal static List<string> BuildStatusLines(UsageState state, DateTimeOffset now)
    {
        if (state.Snapshot is not { } snapshot)
        {
            return [state.Message ?? T.TrayNoData];
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

        return lines.Count == 0 ? [T.TrayNoLimits] : lines;
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
