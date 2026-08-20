using System;
using System.Collections.Generic;

namespace ClaudeUsageChecker.App.Tray;

/// <summary>
/// The icon in the notification area, as the rest of the application needs it.
/// </summary>
/// <remarks>
/// Two implementations, for two genuinely different expectations. Windows draws
/// its context menus in the system font without a frame, which beside the
/// windows of this application looked like a different program - hence
/// <see cref="WindowsTrayIcon"/> plus a window of our own. On macOS the menu bar
/// is the opposite case: a native menu is what belongs there, and anything drawn
/// by hand would be the thing that looks foreign.
/// </remarks>
internal interface ITrayPresenter : IDisposable
{
    /// <summary>
    /// The user clicked the icon without asking for the menu. Windows only -
    /// on macOS a click on the status item opens the menu, which is why the
    /// menu there carries an entry of its own for the details.
    /// </summary>
    event EventHandler? Clicked;

    /// <summary>The hover text. Ignored where the platform has none.</summary>
    void SetToolTip(string text);

    /// <summary>The picture, chosen by how tight the limits are.</summary>
    void SetSeverity(TrayIconSeverity severity);

    /// <summary>
    /// What the menu shows: the reported limits, then the entries to choose.
    /// </summary>
    void SetMenu(
        IReadOnlyList<string> status,
        IReadOnlyList<(string Text, Action Run)> commands);
}
