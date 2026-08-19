using System;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using ClaudeUsageChecker.Core.Authentication;
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
    private Uri? _updateReleasePage;

    public DetailsWindow() : this(TimeProvider.System)
    {
    }

    public DetailsWindow(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
        InitializeComponent();

        RefreshButton.Click += (_, _) => RefreshRequested?.Invoke(this, EventArgs.Empty);
        UpdateButton.Click += (_, _) =>
        {
            if (_updateReleasePage is { } page)
            {
                ReleasePageRequested?.Invoke(this, page);
            }
        };
        InstallButton.Click += (_, _) => InstallRequested?.Invoke(this, EventArgs.Empty);

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

    /// <summary>Der Nutzer moechte die Release-Seite der neuen Version oeffnen.</summary>
    public event EventHandler<Uri>? ReleasePageRequested;

    /// <summary>Der Nutzer moechte die neue Fassung einspielen lassen.</summary>
    public event EventHandler? InstallRequested;

    /// <summary>
    /// Zeigt einen dauerhaften Hinweis an, etwa dass die eigene Anmeldung
    /// abgelaufen ist. Ohne Text wird er ausgeblendet.
    /// </summary>
    public void SetSignInNotice(string? message)
    {
        SignInNoticeText.Text = message;
        SignInNoticeBorder.IsVisible = !string.IsNullOrWhiteSpace(message);
    }

    /// <summary>Stellt den uebergebenen Zustand dar.</summary>
    public void Render(UsageState state)
    {
        var now = _timeProvider.GetLocalNow();

        RenderWindows(state.Snapshot, now);
        RenderExtraUsage(state.Snapshot?.ExtraUsage);
        RenderMessage(state);

        FooterText.Text = state.Snapshot is { } snapshot
            ? string.Format(
                CultureInfo.CurrentCulture,
                "Stand: {0:t} - Quelle: {1}",
                snapshot.RetrievedAt.ToLocalTime(),
                QuellenName(snapshot.TokenSource))
            : "Noch keine Daten";
    }

    /// <summary>
    /// Zeigt das Ergebnis einer Aktualisierungspruefung an. Ohne Text wird der
    /// Hinweis ausgeblendet.
    /// </summary>
    /// <param name="canInstall">
    /// Ob die neue Fassung selbst eingespielt werden kann. Nur dann wird die
    /// entsprechende Schaltflaeche angeboten - ein Knopf, der nicht funktioniert,
    /// ist schlimmer als keiner.
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

    /// <summary>Meldet den Fortschritt des Einspielens.</summary>
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
            Text = "Zusatzkontingent",
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

        // Die API meldet je nach Konto nur einen Teil der Werte - jede Angabe
        // wird nur dann gezeigt, wenn sie tatsaechlich vorliegt.
        var detail = extraUsage switch
        {
            { UsedCredits: { } used, MonthlyLimit: { } limit } => string.Format(
                CultureInfo.CurrentCulture, "{0:0.00} von {1:0.00} Credits verbraucht", used, limit),
            { UsedCredits: { } usedOnly } => string.Format(
                CultureInfo.CurrentCulture, "{0:0.00} Credits verbraucht", usedOnly),
            { MonthlyLimit: { } limitOnly } => string.Format(
                CultureInfo.CurrentCulture, "Monatsgrenze: {0:0.00} Credits", limitOnly),
            _ => "aktiv"
        };

        ExtraUsagePanel.Children.Add(new TextBlock { Text = detail, FontSize = 11, Opacity = 0.7 });
    }

    /// <summary>Sprechender Name der Tokenquelle fuer die Fusszeile.</summary>
    internal static string QuellenName(TokenSource source) => source switch
    {
        TokenSource.OAuth => "eigene Anmeldung",
        TokenSource.SecretStore => "hinterlegtes Token",
        TokenSource.Environment => "Umgebungsvariable",
        TokenSource.ClaudeCli => "Claude Code",
        _ => "unbekannt"
    };

    private void RenderMessage(UsageState state)
    {
        var message = state.Kind switch
        {
            // Zuerst der eigene Weg: Er funktioniert auch dort, wo Claude Code
            // gar nicht installiert ist - und genau dort stand vorher ein
            // Hinweis, dem niemand folgen konnte.
            UsageStateKind.NotConfigured =>
                "Kein Zugriffsrecht vorhanden. Unter Einstellungen → Anmelden holt sich die "
                + "Anwendung ein eigenes. Alternativ liest sie das Token einer angemeldeten "
                + "Claude-Code-Installation mit.",
            // Der Text der Ausnahme unterscheidet bereits zwischen abgelaufenem
            // Token und fehlendem Geltungsbereich - er ist hier hilfreicher als
            // eine allgemeine Formulierung.
            UsageStateKind.AuthenticationFailed => state.Message,
            UsageStateKind.Unavailable => state.Message,
            UsageStateKind.Stale => "Die Anzeige ist veraltet: " + state.Message,
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
                "Reset in {0} - um {1}",
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
