using System;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using ClaudeUsageChecker.Core.Authentication;
using ClaudeUsageChecker.Core.Formatting;
using ClaudeUsageChecker.Core.Localization;
using ClaudeUsageChecker.Core.Models;
using ClaudeUsageChecker.Core.Services;

namespace ClaudeUsageChecker.App.Views;

/// <summary>
/// Compact details window that opens on a click on the tray icon.
/// </summary>
public partial class DetailsWindow : Window
{
    private readonly TimeProvider _timeProvider;
    private Uri? _updateReleasePage;

    public DetailsWindow() : this(TimeProvider.System)
    {
    }

    public DetailsWindow(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
        InitializeComponent();

        // Longer translations grow the window downwards; without this it
        // can end up reaching past the bottom edge of the screen.
        Opened += (_, _) => ScreenFit.Apply(this);
        ApplyTexts();

        RefreshButton.Click += (_, _) => RefreshRequested?.Invoke(this, EventArgs.Empty);
        UpdateButton.Click += (_, _) =>
        {
            if (_updateReleasePage is { } page)
            {
                ReleasePageRequested?.Invoke(this, page);
            }
        };
        InstallButton.Click += (_, _) => InstallRequested?.Invoke(this, EventArgs.Empty);

        // The window behaves like a drop-down: losing focus closes it.
        Deactivated += (_, _) => Hide();
        KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape)
            {
                Hide();
            }
        };
    }

    /// <summary>
    /// Sets every fixed label from the language file.
    /// </summary>
    /// <remarks>
    /// The texts deliberately do not live in the XAML: they have to be
    /// refreshable on a language change, and that only works from here. Called
    /// again after every language change.
    /// </remarks>
    public void ApplyTexts()
    {
        Title = T.AppName;
        InstallButton.Content = T.DetailsInstall;
        UpdateButton.Content = T.DetailsReleasePage;
        RefreshButton.Content = T.Refresh;
    }

    /// <summary>The user asked for an immediate call.</summary>
    public event EventHandler? RefreshRequested;

    /// <summary>The user wants to open the release page of the new version.</summary>
    public event EventHandler<Uri>? ReleasePageRequested;

    /// <summary>The user wants the new version installed.</summary>
    public event EventHandler? InstallRequested;

    /// <summary>
    /// Shows a standing notice, for instance that the application's own sign-in
    /// has expired. Without text it is hidden.
    /// </summary>
    public void SetSignInNotice(string? message)
    {
        SignInNoticeText.Text = message;
        SignInNoticeBorder.IsVisible = !string.IsNullOrWhiteSpace(message);
    }

    /// <summary>Renders the state that was handed in.</summary>
    public void Render(UsageState state)
    {
        var now = _timeProvider.GetLocalNow();

        RenderWindows(state.Snapshot, now);
        RenderExtraUsage(state.Snapshot?.ExtraUsage);
        RenderMessage(state);

        FooterText.Text = state.Snapshot is { } snapshot
            ? T.DetailsFooter(snapshot.RetrievedAt.ToLocalTime(), SourceName(snapshot.TokenSource))
            : T.DetailsNoDataYet;
    }

    /// <summary>
    /// Shows the result of an update check. Without text the notice is hidden.
    /// </summary>
    /// <param name="canInstall">
    /// Whether the new version can be installed by the application itself. Only
    /// then is the button offered - a button that does not work is worse than no
    /// button.
    /// </param>
    public void SetUpdateNotice(string? message, Uri? releasePage = null, bool canInstall = false)
    {
        _updateReleasePage = releasePage;
        UpdateText.Text = message;
        UpdateBorder.IsVisible = !string.IsNullOrWhiteSpace(message);
        UpdateButton.IsVisible = releasePage is not null;
        InstallButton.IsVisible = canInstall;
        InstallButton.IsEnabled = canInstall;
    }

    /// <summary>Reports the progress of the installation.</summary>
    public void SetInstallProgress(string message, bool busy)
    {
        UpdateText.Text = message;
        UpdateBorder.IsVisible = true;
        InstallButton.IsEnabled = !busy;
    }

    private void RenderWindows(UsageSnapshot? snapshot, DateTimeOffset now)
    {
        WindowsPanel.Children.Clear();

        foreach (var (label, window) in UsageFormatter.EnumerateWindows(snapshot))
        {
            WindowsPanel.Children.Add(BuildWindowRow(label, window, now));
        }
    }

    private void RenderExtraUsage(ExtraUsage? extraUsage)
    {
        ExtraUsagePanel.Children.Clear();

        if (extraUsage is not { IsEnabled: true })
        {
            ExtraUsageBorder.IsVisible = false;
            return;
        }

        ExtraUsageBorder.IsVisible = true;
        ExtraUsagePanel.Children.Add(new TextBlock
        {
            Text = T.ExtraTitle,
            FontSize = 12,
            FontWeight = FontWeight.Medium
        });

        if (extraUsage.Utilization is { } utilization)
        {
            ExtraUsagePanel.Children.Add(new ProgressBar
            {
                Minimum = 0,
                Maximum = 100,
                Value = utilization,
                Height = 6,
                Foreground = BrushForUtilization(utilization)
            });
        }

        // Depending on the account the API reports only some of the values - each
        // one is shown only where it is actually present.
        var detail = extraUsage switch
        {
            { UsedCredits: { } used, MonthlyLimit: { } limit } => T.ExtraUsedOfLimitLong(used, limit),
            { UsedCredits: { } usedOnly } => T.ExtraUsedOnly(usedOnly),
            { MonthlyLimit: { } limitOnly } => T.ExtraMonthlyLimit(limitOnly),
            _ => T.ExtraActive
        };

        ExtraUsagePanel.Children.Add(new TextBlock { Text = detail, FontSize = 11, Opacity = 0.7 });
    }

    /// <summary>Readable name of the token source for the footer.</summary>
    internal static string SourceName(TokenSource source) => source switch
    {
        TokenSource.OAuth => T.SourceOAuth,
        TokenSource.SecretStore => T.SourceSecretStore,
        TokenSource.Environment => T.SourceEnvironment,
        TokenSource.ClaudeCli => T.SourceClaudeCli,
        _ => T.Unknown
    };

    private void RenderMessage(UsageState state)
    {
        var message = state.Kind switch
        {
            // The application's own route first: it works even where Claude Code
            // is not installed at all - and it was exactly there that the old
            // notice gave advice nobody could follow.
            UsageStateKind.NotConfigured => T.DetailsNotConfigured,
            // The text of the exception already distinguishes an expired token
            // from a missing scope - it is more helpful here than a general
            // phrasing.
            UsageStateKind.AuthenticationFailed => state.Message,
            UsageStateKind.Unavailable => state.Message,
            UsageStateKind.Stale => T.DetailsStale(state.Message ?? string.Empty),
            _ => null
        };

        MessageText.Text = message;
        MessageBorder.IsVisible = message is not null;
    }

    private static StackPanel BuildWindowRow(string label, UsageWindow window, DateTimeOffset now)
    {
        var header = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };

        var title = new TextBlock { Text = label, FontSize = 12, FontWeight = FontWeight.Medium };
        var value = new TextBlock
        {
            Text = T.Percent(window.Utilization),
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
            Text = T.ResetIn(
                DurationFormatter.ToCompact(window.TimeUntilReset(now)),
                DurationFormatter.ToResetMoment(window.ResetsAt, now)),
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
