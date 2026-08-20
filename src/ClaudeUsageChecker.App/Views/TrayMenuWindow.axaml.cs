using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;

namespace ClaudeUsageChecker.App.Views;

/// <summary>
/// The menu of the notification area, as a window of our own.
/// </summary>
/// <remarks>
/// Windows draws a context menu in the system font, with hairline separators and
/// no frame - beside the other windows of this application it looked like
/// something from another program. Since Avalonia's tray icon offers no way to
/// style that menu, and no right-click event to hang something else on, the icon
/// is registered by the application itself and this window is what a right click
/// opens.
///
/// It behaves like a menu, not like a window: it closes when it loses focus, on
/// Escape, and after any entry has been chosen.
/// </remarks>
public partial class TrayMenuWindow : Window
{
    public TrayMenuWindow()
    {
        InitializeComponent();

        Deactivated += (_, _) => Hide();
        KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape)
            {
                Hide();
            }
        };
    }

    /// <summary>Fills the menu with the lines to show and the entries to offer.</summary>
    /// <param name="status">
    /// The reported limits, one line each. They only say something; nothing is
    /// clickable about them.
    /// </param>
    /// <param name="commands">The entries, in the order they should appear.</param>
    public void Render(
        IReadOnlyList<string> status,
        IReadOnlyList<(string Text, string? Detail, Action Run)> commands)
    {
        ArgumentNullException.ThrowIfNull(status);
        ArgumentNullException.ThrowIfNull(commands);

        StatusPanel.Children.Clear();
        CommandPanel.Children.Clear();

        foreach (var line in status)
        {
            StatusPanel.Children.Add(new TextBlock
            {
                Text = line,
                FontSize = 12,
                Opacity = 0.75,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap
            });
        }

        StatusPanel.IsVisible = status.Count > 0;
        StatusSeparator.IsVisible = status.Count > 0;

        foreach (var (text, detail, run) in commands)
        {
            var button = new Button { Content = BuildEntry(text, detail) };
            button.Classes.Add("menu");
            button.Click += (_, _) =>
            {
                // Away first, then act: several of the entries open a window, and
                // the menu standing behind it would look like an accident.
                Hide();
                run();
            };

            CommandPanel.Children.Add(button);
        }
    }

    /// <summary>
    /// The content of one entry: its text, and to the right the note that goes
    /// with it - the version beside "About", for instance.
    /// </summary>
    private static Control BuildEntry(string text, string? detail)
    {
        var label = new TextBlock { Text = text, VerticalAlignment = VerticalAlignment.Center };

        if (string.IsNullOrEmpty(detail))
        {
            return label;
        }

        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
        var note = new TextBlock
        {
            Text = detail,
            FontSize = 11,
            Opacity = 0.6,
            Margin = new Thickness(12, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center
        };

        Grid.SetColumn(note, 1);
        grid.Children.Add(label);
        grid.Children.Add(note);

        return grid;
    }

    /// <summary>
    /// Opens the menu at the cursor, the way a context menu appears.
    /// </summary>
    /// <remarks>
    /// The size is only known once the layout has run, so the position is
    /// corrected afterwards - the same reason the other windows need
    /// <see cref="ScreenFit"/>.
    /// </remarks>
    public void ShowAt(PixelPoint cursor)
    {
        Show();
        Activate();

        ScreenFit.PlaceAtPointer(this, cursor);
    }
}
