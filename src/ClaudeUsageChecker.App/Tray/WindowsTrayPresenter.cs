using System;
using System.Collections.Generic;
using System.Runtime.Versioning;
using Avalonia;
using ClaudeUsageChecker.App.Views;

namespace ClaudeUsageChecker.App.Tray;

/// <summary>
/// The icon in the Windows notification area: registered by the application
/// itself, with a menu of its own drawn as an ordinary window.
/// </summary>
/// <remarks>
/// Why not Avalonia's tray icon here: its menu under Windows is a real Win32
/// menu, drawn outside the process in the system font with hairline separators
/// and no frame. Beside the other windows of this application it looked like a
/// program from another decade, and nothing inside the process can reach it.
/// See <see cref="WindowsTrayIcon"/> and <see cref="TrayMenuWindow"/>.
/// </remarks>
[SupportedOSPlatform("windows")]
internal sealed class WindowsTrayPresenter : ITrayPresenter
{
    private readonly WindowsTrayIcon _icon = new();

    private TrayMenuWindow? _menu;
    private TrayIconSeverity? _shown;
    private IReadOnlyList<string> _status = [];
    private IReadOnlyList<(string Text, Action Run)> _commands = [];

    public WindowsTrayPresenter()
    {
        _icon.Clicked += (_, _) => Clicked?.Invoke(this, EventArgs.Empty);
        _icon.MenuRequested += (_, cursor) => ShowMenu(cursor);
    }

    public event EventHandler? Clicked;

    public void SetToolTip(string text) => _icon.SetToolTip(text);

    public void SetSeverity(TrayIconSeverity severity)
    {
        // Only on a change: every call builds a new icon handle, and swapping
        // the icon thirty times an hour for the same picture is work for
        // nothing.
        if (_shown == severity)
        {
            return;
        }

        _icon.SetIcon(TrayIconImages.CreateHandle(severity));
        _shown = severity;
    }

    /// <summary>
    /// Keeps what the menu should show. Drawing waits for the right click - the
    /// window is built fresh then anyway, and until then nobody is looking.
    /// </summary>
    public void SetMenu(
        IReadOnlyList<string> status,
        IReadOnlyList<(string Text, Action Run)> commands)
    {
        _status = status ?? throw new ArgumentNullException(nameof(status));
        _commands = commands ?? throw new ArgumentNullException(nameof(commands));
    }

    private void ShowMenu(PixelPoint cursor)
    {
        _menu ??= new TrayMenuWindow();
        _menu.Render(_status, _commands);
        _menu.ShowAt(cursor);
    }

    public void Dispose()
    {
        _menu?.Close();
        _icon.Dispose();
    }
}
