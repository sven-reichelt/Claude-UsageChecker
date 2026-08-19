using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using ClaudeUsageChecker.App.Settings;
using ClaudeUsageChecker.App.Views;
using ClaudeUsageChecker.Core.Models;
using ClaudeUsageChecker.Core.Services;

namespace ClaudeUsageChecker.App.Tests;

/// <summary>
/// Sichert ab, dass die Fenster sich ueberhaupt erzeugen lassen und ihre per
/// x:Name benannten Steuerelemente verknuepft sind.
/// </summary>
/// <remarks>
/// Hintergrund: Eine selbst geschriebene, parameterlose InitializeComponent-Methode
/// verdeckt die von Avalonia erzeugte Fassung. Das XAML wird dann zwar geladen,
/// die Felder der benannten Steuerelemente bleiben aber null - der Konstruktor
/// scheitert mit einer NullReferenceException. Das kompiliert fehlerfrei und faellt
/// erst beim Oeffnen des Fensters auf. Diese Tests fangen genau das ab.
/// </remarks>
public class WindowConstructionTests
{
    [AvaloniaFact]
    public void DetailsWindow_LaesstSichErzeugen()
    {
        var window = new DetailsWindow();

        Assert.NotNull(window.FindControl<Button>("RefreshButton"));
        Assert.NotNull(window.FindControl<StackPanel>("WindowsPanel"));
        Assert.NotNull(window.FindControl<TextBlock>("FooterText"));
    }

    [AvaloniaFact]
    public void DetailsWindow_StelltNutzungsdatenDar()
    {
        var window = new DetailsWindow();

        window.Render(ReadyState());

        var panel = window.FindControl<StackPanel>("WindowsPanel")!;
        Assert.Equal(2, panel.Children.Count);
    }

    [AvaloniaFact]
    public void DetailsWindow_ZeigtHinweisOhneToken()
    {
        var window = new DetailsWindow();

        window.Render(new UsageState { Kind = UsageStateKind.NotConfigured });

        Assert.True(window.FindControl<Border>("MessageBorder")!.IsVisible);
    }

    [AvaloniaFact]
    public void SettingsWindow_LaesstSichErzeugen()
    {
        using var settingsFile = new TemporaryFile();

        var window = new SettingsWindow(
            new FakeSecretStore(),
            new SettingsStore(settingsFile.Path),
            new AppSettings());

        Assert.NotNull(window.FindControl<TextBox>("TokenBox"));
        Assert.NotNull(window.FindControl<NumericUpDown>("IntervalBox"));
        Assert.NotNull(window.FindControl<Button>("SaveButton"));
    }

    [AvaloniaFact]
    public void SettingsWindow_UebernimmtVorhandeneEinstellungen()
    {
        using var settingsFile = new TemporaryFile();
        var settings = new AppSettings { PollIntervalSeconds = 600, CheckForUpdates = false };

        var window = new SettingsWindow(new FakeSecretStore(), new SettingsStore(settingsFile.Path), settings);

        Assert.Equal(600m, window.FindControl<NumericUpDown>("IntervalBox")!.Value);
        Assert.False(window.FindControl<CheckBox>("CheckUpdatesBox")!.IsChecked);
    }

    [AvaloniaFact]
    public void SettingsWindow_MeldetHinterlegtesToken()
    {
        using var settingsFile = new TemporaryFile();
        var store = new FakeSecretStore();
        // Bewusst ohne echte Tokenform - der Geheimnis-Scanner der CI schlaegt sonst an.
        store.Write("ClaudeUsageChecker:OAuthToken", "platzhalter-fuer-test");

        var window = new SettingsWindow(store, new SettingsStore(settingsFile.Path), new AppSettings());

        Assert.Contains("hinterlegt", window.FindControl<TextBlock>("TokenStatus")!.Text!,
            StringComparison.Ordinal);
    }

    [AvaloniaFact]
    public void SettingsWindow_MeldetFehlendenSicherenSpeicher()
    {
        using var settingsFile = new TemporaryFile();
        var store = new FakeSecretStore { IsSupported = false };

        var window = new SettingsWindow(store, new SettingsStore(settingsFile.Path), new AppSettings());

        Assert.False(window.FindControl<Button>("SaveTokenButton")!.IsEnabled);
    }

    private static UsageState ReadyState() => new()
    {
        Kind = UsageStateKind.Ready,
        Snapshot = new UsageSnapshot
        {
            Session = new UsageWindow(33, DateTimeOffset.UtcNow.AddHours(2)),
            Weekly = new UsageWindow(13, DateTimeOffset.UtcNow.AddDays(4)),
            RetrievedAt = DateTimeOffset.UtcNow
        }
    };

    /// <summary>Temporaerer Pfad, damit Tests die Einstellungen des Nutzers nicht anfassen.</summary>
    private sealed class TemporaryFile : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            $"cuc-test-{Guid.NewGuid():N}.json");

        public void Dispose()
        {
            if (File.Exists(Path))
            {
                File.Delete(Path);
            }
        }
    }
}
