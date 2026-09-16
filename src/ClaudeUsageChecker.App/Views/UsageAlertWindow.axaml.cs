using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using ClaudeUsageChecker.App.Settings;
using ClaudeUsageChecker.Core.Formatting;
using ClaudeUsageChecker.Core.Localization;
using ClaudeUsageChecker.Core.Services;

namespace ClaudeUsageChecker.App.Views;

/// <summary>
/// Tells the user that a limit has reached yellow, red, or its end - and when it
/// resets.
/// </summary>
/// <remarks>
/// <para>
/// The icon already changes colour, but an icon in the notification area is
/// easy to miss, and on Windows it often sits in the overflow where nobody sees
/// it at all. A limit running out in the middle of a piece of work is exactly
/// the moment that should not be missed.
/// </para>
/// <para>
/// How insistent it is, is up to the user: whether it waits for "OK, got it!" or
/// goes away by itself, and whether it stays in front of everything else. See
/// <see cref="UsageAlertBehaviour"/>.
/// </para>
/// </remarks>
public partial class UsageAlertWindow : Window
{
    private readonly List<UsageAlert> _alerts = [];
    private readonly UsageAlertBehaviour _behaviour;
    private readonly bool _isPreview;
    private readonly TimeProvider _timeProvider;
    private readonly DispatcherTimer _clock;
    private readonly DispatcherTimer _autoClose;
    private bool _acknowledged;
    private string? _frameBrushKey;

    public UsageAlertWindow() : this(new UsageAlertBehaviour(true, true, TimeSpan.FromSeconds(30)))
    {
    }

    /// <param name="behaviour">Whether it waits for confirmation and stays in front.</param>
    /// <param name="isPreview">Example figures from the settings, to be labelled as such.</param>
    /// <param name="timeProvider">The clock, replaceable in tests.</param>
    public UsageAlertWindow(UsageAlertBehaviour behaviour, bool isPreview = false, TimeProvider? timeProvider = null)
    {
        _behaviour = behaviour ?? throw new ArgumentNullException(nameof(behaviour));
        _isPreview = isPreview;
        _timeProvider = timeProvider ?? TimeProvider.System;

        InitializeComponent();

        Topmost = behaviour.StaysOnTop;

        // The time left keeps running while the notice is open, and a notice
        // may well be open for hours when nobody is at the desk.
        _clock = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
        _clock.Tick += (_, _) => Tick();

        _autoClose = new DispatcherTimer { Interval = behaviour.AutoClose };
        _autoClose.Tick += (_, _) =>
        {
            _autoClose.Stop();
            Close();
        };

        AcknowledgeButton.Click += (_, _) => Acknowledge();

        Opened += (_, _) =>
        {
            _clock.Start();
            RestartAutoClose();
            ScreenFit.Apply(this);
        };

        Closing += (_, e) =>
        {
            if (RefusesToClose(WaitsForConfirmation, e.IsProgrammatic, e.CloseReason))
            {
                e.Cancel = true;
            }
        };

        Closed += (_, _) =>
        {
            _clock.Stop();
            _autoClose.Stop();
        };

        ApplyTexts();
        Render();
    }

    /// <summary>The notices currently shown, one per limit.</summary>
    internal IReadOnlyList<UsageAlert> Alerts => _alerts;

    /// <summary>Whether it is still waiting for confirmation before it may close.</summary>
    internal bool WaitsForConfirmation => _behaviour.RequiresAcknowledgement && !_acknowledged;

    /// <summary>Whether the countdown to closing by itself is running.</summary>
    internal bool IsCountingDown => _autoClose.IsEnabled;

    /// <summary>Whether an attempt to close the notice is turned down.</summary>
    /// <remarks>
    /// Confirming is the only way out where confirming was asked for: Alt+F4 or
    /// its like would otherwise dismiss the notice unread. But never at the
    /// expense of the application or the system shutting down - a notice must
    /// not hold either of them up - and never against the application itself,
    /// which closes it on purpose.
    /// </remarks>
    internal static bool RefusesToClose(bool waitsForConfirmation, bool isProgrammatic, WindowCloseReason reason) =>
        waitsForConfirmation && !isProgrammatic && reason == WindowCloseReason.WindowClosing;

    /// <summary>
    /// Adds notices. One about a limit already shown replaces the earlier one -
    /// a limit that went from yellow to red is one piece of news, not two.
    /// </summary>
    public void Add(IEnumerable<UsageAlert> alerts)
    {
        ArgumentNullException.ThrowIfNull(alerts);

        foreach (var alert in alerts)
        {
            _alerts.RemoveAll(a => a.Key == alert.Key);
            _alerts.Add(alert);
        }

        // The limits in the order they appear everywhere else.
        _alerts.Sort((a, b) => a.Limit != b.Limit
            ? a.Limit.CompareTo(b.Limit)
            : string.CompareOrdinal(a.ModelName, b.ModelName));

        Render();
        RestartAutoClose();
    }

    /// <summary>
    /// Opens the notice in front of whatever the user is doing.
    /// </summary>
    /// <remarks>
    /// Where it is not meant to stay in front, it is still put there once:
    /// Windows refuses the foreground to a process that does not have it, and a
    /// notice that opens behind the work it is about has not told anybody
    /// anything. The flag is dropped straight away, as with the summary of
    /// changes.
    /// </remarks>
    public void Present()
    {
        if (_behaviour.StaysOnTop)
        {
            Show();
            return;
        }

        Topmost = true;
        Show();
        Topmost = false;
    }

    /// <summary>Sets every fixed label from the language file.</summary>
    public void ApplyTexts()
    {
        Title = T.AlertTitle;
        AcknowledgeButton.Content = T.AlertAcknowledge;
        PreviewText.Text = T.AlertPreview;
        AutoCloseText.Text = T.AlertAutoClose((int)_behaviour.AutoClose.TotalSeconds);
        Render();
    }

    /// <summary>
    /// Brings the notice up to date with the clock: drops what has reset, and
    /// counts down what is left.
    /// </summary>
    /// <remarks>
    /// A limit that has reset makes its entry old news. Whoever comes back to the
    /// desk after the session has started over should not find a red notice
    /// telling them it is used up - and have to confirm it before they can get
    /// on. The reset time is known from the start, so the clock decides this on
    /// its own, without waiting for the next call. With nothing left, the notice
    /// closes, even where it was waiting for confirmation: there is nothing left
    /// to confirm.
    /// </remarks>
    internal void Tick()
    {
        var now = _timeProvider.GetUtcNow();
        var hadAlerts = _alerts.Count > 0;

        _alerts.RemoveAll(a => a.Window.ResetsAt <= now);

        if (hadAlerts && _alerts.Count == 0)
        {
            Close();
            return;
        }

        Render();
    }

    private void Acknowledge()
    {
        _acknowledged = true;
        Close();
    }

    private void RestartAutoClose()
    {
        if (_behaviour.RequiresAcknowledgement || !IsVisible)
        {
            return;
        }

        _autoClose.Stop();
        _autoClose.Start();
    }

    private void Render()
    {
        var now = _timeProvider.GetLocalNow();
        var highest = _alerts.Count == 0 ? UsageAlertLevel.Warning : _alerts.Max(a => a.Level);
        var brushKey = BrushKeyFor(highest);

        HeadingText.Text = UsageFormatter.ToAlertHeading(highest);

        // Bound to the resource rather than given a colour, so that a change of
        // appearance reaches an open notice as well. Rebound only when the stage
        // changes - not on every tick of the clock.
        if (brushKey != _frameBrushKey)
        {
            _frameBrushKey = brushKey;
            HeadingText.Bind(TextBlock.ForegroundProperty, this.GetResourceObservable(brushKey));
            Frame.Bind(Border.BorderBrushProperty, this.GetResourceObservable(brushKey));
        }

        PreviewText.IsVisible = _isPreview;
        AutoCloseText.IsVisible = !_behaviour.RequiresAcknowledgement;

        EntriesPanel.Children.Clear();
        foreach (var alert in _alerts)
        {
            EntriesPanel.Children.Add(BuildEntry(alert, now));
        }
    }

    private Border BuildEntry(UsageAlert alert, DateTimeOffset now)
    {
        // The stage shows as a bar beside the entry, not as the colour of its
        // text: yellow lettering on the grey of the entry was hard to read in the
        // light appearance, and a bar says the same at a glance.
        var line = new TextBlock
        {
            Text = T.AlertLine(UsageFormatter.ToAlertLabel(alert), alert.Window.Utilization),
            FontWeight = FontWeight.SemiBold,
            TextWrapping = TextWrapping.Wrap
        };

        var explanation = new TextBlock
        {
            Text = UsageFormatter.ToAlertExplanation(alert),
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap
        };

        var reset = new TextBlock
        {
            Text = UsageFormatter.ToAlertReset(alert.Window, now),
            FontSize = 12,
            Opacity = 0.8,
            TextWrapping = TextWrapping.Wrap
        };

        var entry = new Border
        {
            CornerRadius = new CornerRadius(4),
            BorderThickness = new Thickness(4, 0, 0, 0),
            Padding = new Thickness(10),
            Child = new StackPanel
            {
                Spacing = 4,
                Orientation = Orientation.Vertical,
                Children = { line, explanation, reset }
            }
        };
        entry.Bind(Border.BackgroundProperty, this.GetResourceObservable("SystemControlBackgroundBaseLowBrush"));
        entry.Bind(Border.BorderBrushProperty, this.GetResourceObservable(BrushKeyFor(alert.Level)));

        return entry;
    }

    /// <summary>Yellow for the warning, red for everything beyond - the colours of the icon.</summary>
    private static string BrushKeyFor(UsageAlertLevel level) =>
        level >= UsageAlertLevel.Critical ? "CriticalBrush" : "WarningBrush";
}
