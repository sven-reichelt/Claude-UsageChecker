using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using ClaudeUsageChecker.Core.Formatting;
using ClaudeUsageChecker.Core.Models;
using ClaudeUsageChecker.Core.Services;

namespace ClaudeUsageChecker.App.Views;

/// <summary>
/// Kompakte Detailansicht, die sich beim Klick auf das Infobereich-Symbol oeffnet.
/// </summary>
public partial class DetailsWindow : Window
{
    private readonly TimeProvider _timeProvider;

    public DetailsWindow() : this(TimeProvider.System)
    {
    }

    public DetailsWindow(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
        InitializeComponent();

        RefreshButton.Click += (_, _) => RefreshRequested?.Invoke(this, EventArgs.Empty);

        // Das Fenster verhaelt sich wie ein Aufklappmenue: Fokusverlust schliesst es.
        Deactivated += (_, _) => Hide();
        KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape)
            {
                Hide();
            }
        };
    }

    /// <summary>Der Nutzer hat einen sofortigen Abruf angefordert.</summary>
    public event EventHandler? RefreshRequested;

    /// <summary>Stellt den uebergebenen Zustand dar.</summary>
    public void Render(UsageState state)
    {
        var now = _timeProvider.GetLocalNow();
        WindowsPanel.Children.Clear();

        foreach (var (label, window) in EnumerateWindows(state.Snapshot))
        {
            WindowsPanel.Children.Add(BuildWindowRow(label, window, now));
        }

        var message = state.Kind switch
        {
            UsageStateKind.NotConfigured =>
                "Kein Token hinterlegt. Bitte in den Einstellungen ein Token aus "
                + "\"claude setup-token\" eintragen.",
            UsageStateKind.AuthenticationFailed =>
                "Das Token wurde abgelehnt. Bitte ein neues Token hinterlegen.",
            UsageStateKind.Unavailable => state.Message,
            UsageStateKind.Stale => "Die Anzeige ist veraltet: " + state.Message,
            _ => null
        };

        MessageText.Text = message;
        MessageBorder.IsVisible = message is not null;

        FooterText.Text = state.Snapshot is { } snapshot
            ? string.Format(
                CultureInfo.CurrentCulture,
                "Stand: {0:t}",
                snapshot.RetrievedAt.ToLocalTime())
            : "Noch keine Daten";
    }

    private static IEnumerable<(string Label, UsageWindow Window)> EnumerateWindows(UsageSnapshot? snapshot)
    {
        if (snapshot is null)
        {
            yield break;
        }

        if (snapshot.Session is { } session)
        {
            yield return ("Sitzung (5 Std)", session);
        }

        if (snapshot.Weekly is { } weekly)
        {
            yield return ("Woche gesamt", weekly);
        }

        if (snapshot.WeeklyOpus is { } opus)
        {
            yield return ("Woche Opus", opus);
        }

        if (snapshot.WeeklySonnet is { } sonnet)
        {
            yield return ("Woche Sonnet", sonnet);
        }
    }

    private static StackPanel BuildWindowRow(string label, UsageWindow window, DateTimeOffset now)
    {
        var header = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto")
        };

        var title = new TextBlock { Text = label, FontSize = 12, FontWeight = FontWeight.Medium };
        var value = new TextBlock
        {
            Text = string.Format(CultureInfo.CurrentCulture, "{0:0.#} %", window.Utilization),
            FontSize = 12,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        Grid.SetColumn(value, 1);
        header.Children.Add(title);
        header.Children.Add(value);

        var bar = new ProgressBar
        {
            Minimum = 0,
            Maximum = 100,
            Value = window.Utilization,
            Height = 6,
            Foreground = BrushForUtilization(window.Utilization)
        };

        var reset = new TextBlock
        {
            Text = string.Format(
                CultureInfo.CurrentCulture,
                "Reset in {0} - um {1:t}",
                DurationFormatter.ToCompact(window.TimeUntilReset(now)),
                window.ResetsAt.ToLocalTime()),
            FontSize = 11,
            Opacity = 0.7
        };

        return new StackPanel
        {
            Spacing = 4,
            Children = { header, bar, reset }
        };
    }

    private static SolidColorBrush BrushForUtilization(double utilization) => utilization switch
    {
        >= 90d => new SolidColorBrush(Color.FromRgb(0xD0, 0x40, 0x40)),
        >= 75d => new SolidColorBrush(Color.FromRgb(0xE0, 0xA0, 0x30)),
        _ => new SolidColorBrush(Color.FromRgb(0xD9, 0x77, 0x57))
    };
}
