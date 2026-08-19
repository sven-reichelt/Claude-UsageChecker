using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using ClaudeUsageChecker.App.Views;
using ClaudeUsageChecker.Core.Models;
using ClaudeUsageChecker.Core.Services;

namespace ClaudeUsageChecker.App.Tests;

/// <summary>
/// Prueft, dass der Inhalt in das Fenster passt.
/// </summary>
/// <remarks>
/// Anlass war ein Ueberlauf: Zwei Schaltflaechen nebeneinander brauchten mehr
/// Platz, als das 380 Pixel breite Fenster hergibt - die zweite ragte hinaus
/// und war nur halb lesbar. So etwas faellt in keinem Funktionstest auf, weil
/// alle Steuerelemente vorhanden und bedienbar sind. Nur die Vermessung zeigt es.
/// </remarks>
public class DetailsWindowLayoutTests
{
    [AvaloniaFact]
    public void DerUpdateHinweisPasstInsFenster()
    {
        var window = new DetailsWindow();
        window.Render(ReadyState());
        window.SetUpdateNotice(
            "Version 0.9.9 ist verfügbar (installiert: 0.3.0).",
            new Uri("https://example.invalid/release"),
            canInstall: true);

        Assert.True(PasstInDieBreite(window, out var breite),
            $"Der Inhalt braucht {breite:0} Pixel, das Fenster ist {window.Width:0} breit.");
    }

    [AvaloniaFact]
    public void AuchDerHinweisAufEineAbgelaufeneAnmeldungPasst()
    {
        var window = new DetailsWindow();
        window.Render(ReadyState());
        window.SetSignInNotice(
            "Die eigene Anmeldung ist abgelaufen und wurde entfernt. "
            + "Bitte in den Einstellungen neu anmelden.");

        Assert.True(PasstInDieBreite(window, out var breite),
            $"Der Inhalt braucht {breite:0} Pixel, das Fenster ist {window.Width:0} breit.");
    }

    [AvaloniaFact]
    public void AlleVierNutzungsfensterPassen()
    {
        var window = new DetailsWindow();
        var now = DateTimeOffset.UtcNow;

        window.Render(new UsageState
        {
            Kind = UsageStateKind.Ready,
            Snapshot = new UsageSnapshot
            {
                Session = new UsageWindow(100, now.AddHours(1)),
                Weekly = new UsageWindow(100, now.AddDays(3)),
                WeeklyOpus = new UsageWindow(100, now.AddDays(3)),
                WeeklySonnet = new UsageWindow(100, now.AddDays(3)),
                ExtraUsage = new ExtraUsage(true, 999.99m, 888.88m, 99.9),
                RetrievedAt = now
            }
        });

        Assert.True(PasstInDieBreite(window, out var breite),
            $"Der Inhalt braucht {breite:0} Pixel, das Fenster ist {window.Width:0} breit.");
    }

    /// <summary>
    /// Vermisst den Inhalt mit der tatsaechlichen Fensterbreite und sucht dann
    /// nach Elementen, die trotz dieser Vorgabe mehr Platz beanspruchen.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Nicht das Fenster vermessen: Dessen Breite steht fest, es meldet also
    /// stets 380 zurueck, egal wie viel darin ueberlaeuft.
    /// </para>
    /// <para>
    /// Und nicht ohne Beschraenkung messen: Umbrechende Textbloecke melden dann
    /// ihre volle Zeilenlaenge, was jeden laengeren Satz faelschlich als
    /// Ueberlauf ausweisen wuerde. Mit Vorgabe fuegen sie sich, waehrend eine
    /// Reihe nebeneinanderliegender Schaltflaechen ihren Bedarf unveraendert
    /// meldet - genau das soll auffallen.
    /// </para>
    /// </remarks>
    private static bool PasstInDieBreite(Window window, out double breite)
    {
        window.Show();

        breite = 0;
        var passt = true;

        foreach (var kind in window.GetLogicalDescendants().OfType<Control>())
        {
            if (!kind.IsVisible || kind.Bounds.Width <= 0)
            {
                continue;
            }

            // Der rechte Rand des Elements, umgerechnet in Fensterkoordinaten.
            if (kind.TranslatePoint(new Point(kind.Bounds.Width, 0), window) is not { } rechts)
            {
                continue;
            }

            breite = Math.Max(breite, rechts.X);
            if (rechts.X > window.Width + Toleranz)
            {
                passt = false;
            }
        }

        window.Hide();
        return passt;
    }

    /// <summary>Rundungsspielraum des Layouts, in Pixeln.</summary>
    private const double Toleranz = 0.5;

    private static UsageState ReadyState() => new()
    {
        Kind = UsageStateKind.Ready,
        Snapshot = new UsageSnapshot
        {
            Session = new UsageWindow(7, DateTimeOffset.UtcNow.AddHours(3)),
            Weekly = new UsageWindow(16, DateTimeOffset.UtcNow.AddDays(3)),
            RetrievedAt = DateTimeOffset.UtcNow
        }
    };
}
