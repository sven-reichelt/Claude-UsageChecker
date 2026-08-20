using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Threading;
using ClaudeUsageChecker.App.Services;
using ClaudeUsageChecker.App.Settings;
using ClaudeUsageChecker.App.Views;
using ClaudeUsageChecker.Core.Formatting;
using ClaudeUsageChecker.Core.Localization;
using ClaudeUsageChecker.Core.Models;
using ClaudeUsageChecker.Core.Services;

namespace ClaudeUsageChecker.App.Tray;

/// <summary>
/// Connects the <see cref="UsageMonitor"/> to the icon in the notification
/// area: keeps tooltip, colour and menu up to date.
/// </summary>
/// <remarks>
/// The icon is registered with Windows by the application itself rather than
/// through Avalonia's <c>TrayIcon</c>. The reason is the menu: Avalonia offers
/// only a <c>NativeMenu</c>, which under Windows is a real Win32 menu that
/// cannot be styled from inside the process - system font, hairline separators,
/// no frame. Beside the other windows it looked like a different program. See
/// <see cref="WindowsTrayIcon"/>.
/// </remarks>
public sealed class TrayIconController : IDisposable
{
    /// <summary>
    /// How many status lines the menu is expected to show: session, weekly
    /// total, the extra usage, and room for up to five model-specific weekly
    /// limits.
    /// </summary>
    /// <remarks>
    /// Not a hard limit any more - the menu is a window and shows whatever it is
    /// given. It stays as a statement of what is expected: a menu that suddenly
    /// listed a dozen limits would have stopped being a menu, and
    /// <c>ThereAreEnoughSlotsForEveryReportedLimit</c> would say so.
    /// </remarks>
    internal const int StatusSlotCount = 8;

    private readonly UsageMonitor _monitor;
    private readonly Func<AppSettings> _settings;
    private readonly TimeProvider _timeProvider;

    private readonly WindowsTrayIcon _icon;
    private readonly DispatcherTimer _tooltipTimer;

    private TrayMenuWindow? _menu;
    private TrayIconSeverity? _shown;

    public TrayIconController(
        UsageMonitor monitor,
        Func<AppSettings> settings,
        TimeProvider? timeProvider = null)
    {
        _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _timeProvider = timeProvider ?? TimeProvider.System;

        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException(
                "The notification area is only implemented for Windows. The macOS menu bar is still open.");
        }

        _icon = new WindowsTrayIcon();
        _icon.Clicked += (_, _) => ShowDetails?.Invoke(this, EventArgs.Empty);
        _icon.MenuRequested += (_, cursor) => ShowMenu(cursor);

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

    /// <summary>Relabels what outlives a language change.</summary>
    /// <remarks>
    /// The menu needs nothing here: its entries are built afresh every time it
    /// opens, so they carry the language of that moment by themselves. Only the
    /// tooltip is standing text.
    /// </remarks>
    public void ApplyTexts() => Render(_monitor.State);

    private void ShowMenu(PixelPoint cursor)
    {
        _menu ??= new TrayMenuWindow();

        _menu.Render(
            BuildStatusLines(_monitor.State, _timeProvider.GetLocalNow()),
            // No "show details": a left click on the icon opens the details
            // window, and the figures are already in the status lines above. An
            // entry offering the same route again makes the menu longer without
            // adding anything.
            [
                (T.TrayRefreshNow, () => RefreshRequested?.Invoke(this, EventArgs.Empty)),
                (T.TraySettings, () => ShowSettings?.Invoke(this, EventArgs.Empty)),
                (T.TrayCheckForUpdates, () => CheckForUpdatesRequested?.Invoke(this, EventArgs.Empty)),
                // The version stands in the entry itself: it leads to the
                // window that states it, and anyone reporting a problem is
                // asked for it before anything else.
                (T.TrayAbout(Version()), () => ShowAboutRequested?.Invoke(this, EventArgs.Empty)),
                (T.TrayExit, () => ExitRequested?.Invoke(this, EventArgs.Empty))
            ]);

        _menu.ShowAt(cursor);
    }

    /// <summary>
    /// The running version, three parts, for the note beside "About".
    /// </summary>
    /// <remarks>
    /// Anyone reporting a problem is asked for it first, and until now it could
    /// only be found by opening a window. Beside the entry that leads there it
    /// costs nothing and saves the trip.
    /// </remarks>
    private static string Version() => ProgramVersion.Current.ToString();

    private void OnStateChanged(object? sender, UsageState state) =>
        Dispatcher.UIThread.Post(() => Render(state));

    private void Render(UsageState state)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var now = _timeProvider.GetLocalNow();
        var settings = _settings();

        _icon.SetToolTip(UsageFormatter.ToTooltip(state, now));

        var severity = TrayIconSeverityResolver.Resolve(
            state, settings.WarningThreshold, settings.CriticalThreshold);

        // Only on a change: every call builds a new icon handle, and swapping
        // the icon thirty times an hour for the same picture is work for
        // nothing.
        if (_shown != severity)
        {
            _icon.SetIcon(TrayIconImages.CreateHandle(severity));
            _shown = severity;
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

    public void Dispose()
    {
        _tooltipTimer.Stop();
        _monitor.StateChanged -= OnStateChanged;

        _menu?.Close();

        if (OperatingSystem.IsWindows())
        {
            _icon.Dispose();
        }
    }
}
