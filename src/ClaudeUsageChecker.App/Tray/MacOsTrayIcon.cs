using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using Avalonia.Controls;

namespace ClaudeUsageChecker.App.Tray;

/// <summary>
/// The status item in the macOS menu bar, through Avalonia's
/// <see cref="TrayIcon"/> and a native menu.
/// </summary>
/// <remarks>
/// <para>
/// The opposite decision to Windows, for the same reason: what belongs there.
/// A macOS menu bar item opens an ordinary menu, drawn by the system in the
/// system font, and a window painted to look like one would be the thing that
/// stood out. Avalonia's tray icon does exactly this and nothing more, which
/// here is all that is wanted.
/// </para>
/// <para>
/// It follows that there is no left click to catch: a click on a status item
/// with a menu opens that menu. The details window therefore needs an entry of
/// its own, which the controller adds on this platform.
/// </para>
/// </remarks>
[SupportedOSPlatform("macos")]
internal sealed class MacOsTrayIcon : ITrayPresenter
{
    private readonly TrayIcon _icon = new();
    private readonly TrayIcons _icons;

    /// <summary>
    /// One menu for the whole run, refilled rather than replaced.
    /// </summary>
    /// <remarks>
    /// Handing the tray icon a different NativeMenu after the first one has
    /// been exported throws: "The menu being updated does not match". The
    /// exporter on macOS remembers the object it handed to the system and will
    /// only be told about changes to that one. Found by starting the built
    /// bundle in the release workflow, which is the only place this could show
    /// up before the machine existed.
    /// </remarks>
    private readonly NativeMenu _menu = new();

    private TrayIconSeverity? _shown;
    private string? _rendered;

    public MacOsTrayIcon()
    {
        _icons = [_icon];

        // Through the application rather than on its own: this is the route
        // Avalonia documents, and it ties the item's life to the application's
        // instead of leaving it to the garbage collector.
        if (Avalonia.Application.Current is { } application)
        {
            TrayIcon.SetIcons(application, _icons);
        }

        _icon.Menu = _menu;
        _icon.IsVisible = true;
    }

    /// <summary>Never raised here - see the remarks on the class.</summary>
    public event EventHandler? Clicked
    {
        add { }
        remove { }
    }

    public void SetToolTip(string text) => _icon.ToolTipText = text;

    public void SetSeverity(TrayIconSeverity severity)
    {
        // Only on a change: loading the picture again for the same state is
        // work for nothing, and the menu bar would redraw for it.
        if (_shown == severity)
        {
            return;
        }

        _icon.Icon = TrayIconImages.Load(severity);
        _shown = severity;
    }

    public void SetMenu(
        IReadOnlyList<string> status,
        IReadOnlyList<(string Text, Action Run)> commands)
    {
        ArgumentNullException.ThrowIfNull(status);
        ArgumentNullException.ThrowIfNull(commands);

        // A native menu is exported to the system as a whole. Rebuilding it
        // while it is open would pull the ground from under the pointer, so it
        // happens only when something actually reads differently.
        var signature = string.Join('\n', status.Concat(commands.Select(c => c.Text)));
        if (_rendered == signature)
        {
            return;
        }

        _menu.Items.Clear();

        foreach (var line in status)
        {
            // The reported limits say something and do nothing. Disabled is how
            // macOS shows exactly that.
            _menu.Items.Add(new NativeMenuItem(line) { IsEnabled = false });
        }

        if (status.Count > 0)
        {
            _menu.Items.Add(new NativeMenuItemSeparator());
        }

        foreach (var (text, run) in commands)
        {
            var item = new NativeMenuItem(text);
            item.Click += (_, _) => run();
            _menu.Items.Add(item);
        }

        _rendered = signature;
    }

    public void Dispose()
    {
        _icon.IsVisible = false;
        _icon.Dispose();
    }
}
