using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using ClaudeUsageChecker.App.Services;
using ClaudeUsageChecker.App.Settings;
using ClaudeUsageChecker.App.Views;
using ClaudeUsageChecker.Core.Localization;
using ClaudeUsageChecker.Core.Models;
using ClaudeUsageChecker.Core.Services;

namespace ClaudeUsageChecker.App.Tests;

/// <summary>
/// Checks that every window is fully labelled in every language.
/// </summary>
/// <remarks>
/// The texts no longer live in the XAML but are set in code. If a control is
/// forgotten there, it simply stays empty - the window opens, everything is
/// operable, only one spot says nothing. No functional test notices that. These
/// tests walk every window in every language and look for exactly it.
/// </remarks>
public class LabellingTests : IDisposable
{
    private readonly Language _before = Localizer.Current.Language;

    public void Dispose()
    {
        Localizer.Use(_before);
        GC.SuppressFinalize(this);
    }

    public static TheoryData<string> Languages()
    {
        var data = new TheoryData<string>();
        foreach (var language in Language.All)
        {
            data.Add(language.Code);
        }

        return data;
    }

    [AvaloniaTheory]
    [MemberData(nameof(Languages))]
    public void TheDetailsWindowIsFullyLabelled(string code)
    {
        Localizer.Use(Language.Find(code)!);

        var window = new DetailsWindow();
        window.Render(ReadyState());
        window.SetUpdateNotice("...", new Uri("https://example.invalid/r"), canInstall: true);

        AssertFullyLabelled(window, code);
    }

    [AvaloniaTheory]
    [MemberData(nameof(Languages))]
    public void TheSettingsWindowIsFullyLabelled(string code)
    {
        Localizer.Use(Language.Find(code)!);

        using var file = new TemporaryFile();
        var window = new SettingsWindow(
            new SettingsStore(file.Path), new AppSettings(),
            applyAutostart: _ => { });

        AssertFullyLabelled(window, code);
    }

    [AvaloniaTheory]
    [MemberData(nameof(Languages))]
    public void TheSignInWindowIsFullyLabelled(string code)
    {
        Localizer.Use(Language.Find(code)!);

        AssertFullyLabelled(new SignInWindow(), code);
    }

    [AvaloniaTheory]
    [MemberData(nameof(Languages))]
    public void TheSetupWindowIsFullyLabelled(string code)
    {
        Localizer.Use(Language.Find(code)!);

        AssertFullyLabelled(new InstallPromptWindow(), code);
    }

    [AvaloniaTheory]
    [MemberData(nameof(Languages))]
    public void TheAboutWindowIsFullyLabelled(string code)
    {
        Localizer.Use(Language.Find(code)!);

        var window = new AboutWindow(new Uri("https://example.invalid/repo"), new ProgramVersion(new Version(0, 6, 0)));

        AssertFullyLabelled(window, code);
    }

    [AvaloniaTheory]
    [MemberData(nameof(Languages))]
    public void TheReleaseNotesWindowIsFullyLabelled(string code)
    {
        Localizer.Use(Language.Find(code)!);

        var window = new ReleaseNotesWindow();
        window.Render([], new Version(0, 5, 0));

        AssertFullyLabelled(window, code);
    }

    /// <summary>
    /// A language change has to reach the parts that outlive it as well -
    /// otherwise the application would stand half in one language and half in
    /// the other.
    /// </summary>
    [AvaloniaFact]
    public void ALanguageChangeReachesTheExistingDetailsWindow()
    {
        Localizer.Use(Language.Default);
        var window = new DetailsWindow();
        var before = window.FindControl<Button>("RefreshButton")!.Content as string;

        Localizer.Use(Language.Find("de")!);
        window.ApplyTexts();

        var after = window.FindControl<Button>("RefreshButton")!.Content as string;

        Assert.Equal("Refresh", before);
        Assert.Equal("Aktualisieren", after);
    }

    /// <summary>
    /// The usage windows take their label from the language file; the model name
    /// within comes from the API and stays unchanged.
    /// </summary>
    [AvaloniaFact]
    public void TheModelNameStaysPutInEveryLanguage()
    {
        Localizer.Use(Language.Find("ru")!);

        var window = new DetailsWindow();
        window.Render(ReadyState());

        var texte = window.GetLogicalDescendants().OfType<TextBlock>()
            .Select(t => t.Text ?? string.Empty).ToList();

        Assert.Contains(texte, t => t.Contains("Fable", StringComparison.Ordinal));
        Assert.Contains(texte, t => t.Contains("Сессия", StringComparison.Ordinal));
    }

    /// <summary>
    /// Looks for visible controls that carry no text.
    /// </summary>
    private static void AssertFullyLabelled(Window window, string code)
    {
        window.Show();

        var empty = new List<string>();

        foreach (var control in window.GetLogicalDescendants().OfType<Control>())
        {
            if (!control.IsVisible)
            {
                continue;
            }

            switch (control)
            {
                // Named elements only: an unnamed TextBlock is filled by the
                // application itself (a status line, say) and may well be empty.
                case TextBlock { Name: { } textName } t when string.IsNullOrEmpty(t.Text) && IsFixedLabel(textName):
                    empty.Add($"TextBlock {textName}");
                    break;

                case ContentControl { Name: { } buttonName } c
                    when c is Button or CheckBox && c.Content is null or "":
                    empty.Add($"{c.GetType().Name} {buttonName}");
                    break;
            }
        }

        window.Hide();

        Assert.True(empty.Count == 0,
            $"In {window.GetType().Name} ({code}) these labels are empty: {string.Join(", ", empty)}");
    }

    /// <summary>
    /// Whether a named TextBlock carries a fixed label.
    /// </summary>
    /// <remarks>
    /// The others display state - error messages, notices, status lines - and
    /// are rightly empty at rest.
    /// </remarks>
    private static bool IsFixedLabel(string name) =>
        !name.EndsWith("Status", StringComparison.Ordinal)
        && !name.EndsWith("Text", StringComparison.Ordinal)
        && !name.EndsWith("Hint", StringComparison.Ordinal)
        && name is not ("UrlHint" or "SubtitleText" or "VersionText" or "RepositoryText" or "LicenseText");

    private static UsageState ReadyState()
    {
        var now = DateTimeOffset.UtcNow;

        return new UsageState
        {
            Kind = UsageStateKind.Ready,
            Snapshot = new UsageSnapshot
            {
                Session = new UsageWindow(6, now.AddHours(3)),
                Weekly = new UsageWindow(18, now.AddDays(3)),
                ScopedWeekly = [new ScopedUsageWindow("Fable", new UsageWindow(2, now.AddDays(3)))],
                ExtraUsage = new ExtraUsage(true, 50m, 12m, 24d),
                RetrievedAt = now
            }
        };
    }

    private sealed class TemporaryFile : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(), $"cuc-test-{Guid.NewGuid():N}.json");

        public void Dispose()
        {
            if (File.Exists(Path))
            {
                File.Delete(Path);
            }
        }
    }
}
