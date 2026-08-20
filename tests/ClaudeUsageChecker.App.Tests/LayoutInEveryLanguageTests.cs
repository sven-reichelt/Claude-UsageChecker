using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Avalonia.Headless.XUnit;
using ClaudeUsageChecker.App.Services;
using ClaudeUsageChecker.App.Settings;
using ClaudeUsageChecker.App.Tray;
using ClaudeUsageChecker.App.Views;
using ClaudeUsageChecker.Core.Authentication;
using ClaudeUsageChecker.Core.Localization;
using T = ClaudeUsageChecker.Core.Localization.T;
using ClaudeUsageChecker.Core.Models;
using ClaudeUsageChecker.Core.Services;

namespace ClaudeUsageChecker.App.Tests;

/// <summary>
/// Measures every window in every language and looks for content that runs past
/// its edge.
/// </summary>
/// <remarks>
/// <para>
/// The windows have a fixed width, so a longer translation cannot widen them -
/// it overflows instead. French and Russian run noticeably longer than the
/// English source, Chinese shorter, and a row of buttons beside each other does
/// not wrap the way a sentence does.
/// </para>
/// <para>
/// Until now this was checked by eye, in whichever language happened to be set,
/// and the width was measured in English only. That leaves eight languages
/// unchecked and needs a person for every look. What a test can measure it
/// should measure - the eye is then left for what it alone can judge: whether a
/// line break sits where it reads well.
/// </para>
/// <para>
/// Height is not asserted here. On the headless platform there is no screen
/// worth speaking of; the windows that grow with their content have their own
/// tests for that, against the working area of a real screen.
/// </para>
/// </remarks>
public class LayoutInEveryLanguageTests : IDisposable
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
    public void TheDetailsWindowFitsInEveryLanguage(string code)
    {
        Localizer.Use(Language.Find(code)!);

        var window = new DetailsWindow();
        window.Render(ReadyState());
        window.SetUpdateNotice("0.6.2", new Uri("https://example.invalid/r"), canInstall: true);

        AssertFits(window, code);
    }

    [AvaloniaTheory]
    [MemberData(nameof(Languages))]
    public void TheSettingsWindowFitsInEveryLanguage(string code)
    {
        Localizer.Use(Language.Find(code)!);

        using var file = new TemporaryFile();
        var window = new SettingsWindow(
            new SettingsStore(file.Path), new AppSettings(), applyAutostart: _ => { });

        AssertFits(window, code);
    }

    [AvaloniaTheory]
    [MemberData(nameof(Languages))]
    public void TheSignInWindowFitsInEveryLanguage(string code)
    {
        Localizer.Use(Language.Find(code)!);

        AssertFits(new SignInWindow(), code);
    }

    [AvaloniaTheory]
    [MemberData(nameof(Languages))]
    public void TheSetupWindowFitsInEveryLanguage(string code)
    {
        Localizer.Use(Language.Find(code)!);

        AssertFits(new InstallPromptWindow(), code);
    }

    [AvaloniaTheory]
    [MemberData(nameof(Languages))]
    public void TheAboutWindowFitsInEveryLanguage(string code)
    {
        Localizer.Use(Language.Find(code)!);

        AssertFits(new AboutWindow(new Uri("https://example.invalid/repo"), new ProgramVersion(new Version(0, 6, 1))), code);
    }

    /// <summary>
    /// The changelog window with the whole changelog, which is the widest thing
    /// it will ever have to show.
    /// </summary>
    [AvaloniaTheory]
    [MemberData(nameof(Languages))]
    public void TheReleaseNotesWindowFitsInEveryLanguage(string code)
    {
        Localizer.Use(Language.Find(code)!);

        var window = new ReleaseNotesWindow();
        window.Render(ChangelogResource.All(), new Version(0, 1, 0), ChangelogResource.IsTranslated);

        AssertFits(window, code);
    }

    /// <summary>
    /// The extra usage quota, whose amounts differ in length by currency.
    /// </summary>
    /// <remarks>
    /// "1.234,50 EUR" is longer than "12.50 USD", and the line sits beside a
    /// progress bar in a window that cannot widen.
    /// </remarks>
    [AvaloniaTheory]
    [MemberData(nameof(Languages))]
    public void TheExtraUsageLineFitsInEveryLanguage(string code)
    {
        Localizer.Use(Language.Find(code)!);

        var state = ReadyState(new ExtraUsage(
            IsEnabled: true, Used: 1234.56m, Limit: 9999.99m, Utilization: 12.3d,
            Currency: "EUR", Decimals: 2));

        var window = new DetailsWindow();
        window.Render(state);

        AssertFits(window, code);
    }

    [AvaloniaTheory]
    [MemberData(nameof(Languages))]
    public void TheTrayMenuFitsInEveryLanguage(string code)
    {
        Localizer.Use(Language.Find(code)!);

        AssertFits(BuildTrayMenu(), code);
    }

    /// <summary>
    /// No reported limit is broken across two lines in the menu.
    /// </summary>
    /// <remarks>
    /// Fitting is not enough here: the lines wrap, so a line too long does not
    /// overflow, it folds - and a folded line in a menu of one-line entries reads
    /// like two limits where there is one. The extra usage line is the long one,
    /// because it carries two amounts and a currency; it was missing from the
    /// fixture entirely, which is why nobody noticed it folding.
    /// </remarks>
    [AvaloniaTheory]
    [MemberData(nameof(Languages))]
    public void TheTrayMenuKeepsEveryLimitOnOneLine(string code)
    {
        Localizer.Use(Language.Find(code)!);

        var window = BuildTrayMenu();
        window.Show();

        // Without this the layout has not run and every block reports a height
        // of zero - the check would pass on anything.
        Dispatcher.UIThread.RunJobs();

        var folded = window.GetLogicalDescendants().OfType<TextBlock>()
            .Where(t => t.IsVisible && t.TextLayout.TextLines.Count > 1)
            .Select(t => t.Text)
            .ToList();

        window.Hide();

        Assert.True(folded.Count == 0,
            $"In {code} the menu folds: {string.Join(" | ", folded)}");
    }

    /// <summary>
    /// The menu of the notification area, filled the way the application fills
    /// it: the reported limits above, the entries below.
    /// </summary>
    internal static TrayMenuWindow BuildTrayMenu()
    {
        var window = new TrayMenuWindow();

        // With the extra usage quota: it is the longest of the lines, carrying
        // two amounts and a currency. The fixture left it out entirely, which is
        // why nobody noticed it folding onto a second line.
        var state = ReadyState(new ExtraUsage(
            IsEnabled: true, Used: 1234.56m, Limit: 9999.99m, Utilization: 12.3d,
            Currency: "EUR", Decimals: 2));

        window.Render(
            TrayIconController.BuildStatusLines(state, DateTimeOffset.UtcNow),
            [
                (T.TrayRefreshNow, () => { }),
                (T.TraySettings, () => { }),
                (T.TrayCheckForUpdates, () => { }),
                (T.TrayAbout(ProgramVersion.Current.ToString()), () => { }),
                (T.TrayExit, () => { })
            ]);

        return window;
    }

    private static void AssertFits(Window window, string code)
    {
        Assert.True(LayoutProbe.FitsTheWidth(window, out var width),
            $"{window.GetType().Name} in {code}: the content needs {width:0} pixels, "
            + $"the window is {window.Width:0} wide.");
    }

    private static UsageState ReadyState(ExtraUsage? extraUsage = null)
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
                ExtraUsage = extraUsage,
                RetrievedAt = now,
                TokenSource = TokenSource.OAuth
            }
        };
    }

    private sealed class TemporaryFile : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(), $"cuc-layout-{Guid.NewGuid():N}.json");

        public void Dispose()
        {
            if (File.Exists(Path))
            {
                File.Delete(Path);
            }
        }
    }
}
