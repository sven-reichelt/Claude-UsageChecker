using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using ClaudeUsageChecker.App.Services;
using ClaudeUsageChecker.App.Settings;
using ClaudeUsageChecker.App.Views;
using ClaudeUsageChecker.Core.Authentication;
using ClaudeUsageChecker.Core.Localization;
using ClaudeUsageChecker.Core.Models;
using ClaudeUsageChecker.Core.Services;

namespace ClaudeUsageChecker.App.Tests;

/// <summary>
/// Draws every window in every language and keeps the picture.
/// </summary>
/// <remarks>
/// <para>
/// Measuring catches content that runs past an edge; it says nothing about
/// whether a window is actually drawable. A missing image, a font that cannot
/// render a script, a brush that resolves to nothing - none of that shows up in
/// a layout measurement, and on the headless stub it would not even be
/// attempted, because the stub skips drawing altogether.
/// </para>
/// <para>
/// Set <c>CUC_RENDER_DIR</c> to have the frames written there as PNG. That turns
/// the visual pass into something that can be done without clicking through nine
/// languages by hand - useful above all for Russian and Chinese, where nobody
/// here can judge the text but everybody can see a broken glyph.
/// </para>
/// </remarks>
public class WindowRenderingTests : IDisposable
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
    public void EveryWindowDraws(string code)
    {
        Localizer.Use(Language.Find(code)!);

        using var file = new TemporaryFile();

        Capture(BuildDetails(), $"details-{code}");
        Capture(
            new SettingsWindow(new SettingsStore(file.Path), new AppSettings(), applyAutostart: _ => { }),
            $"settings-{code}");
        Capture(new SignInWindow(), $"signin-{code}");
        Capture(new InstallPromptWindow(), $"setup-{code}");
        Capture(
            new AboutWindow(new Uri("https://example.invalid/repo"), new Version(0, 6, 1)),
            $"about-{code}");
        Capture(BuildReleaseNotes(), $"notes-{code}");
    }

    /// <summary>
    /// Draws the window and checks that something came out of it.
    /// </summary>
    /// <remarks>
    /// An all-white or empty frame means the window did not really render - the
    /// point of drawing it at all.
    /// </remarks>
    private static void Capture(Window window, string name)
    {
        window.Show();

        var frame = window.CaptureRenderedFrame();
        Assert.NotNull(frame);
        Assert.True(frame.Size.Width > 0 && frame.Size.Height > 0,
            $"{name} rendered an empty frame.");

        if (Environment.GetEnvironmentVariable("CUC_RENDER_DIR") is { Length: > 0 } directory)
        {
            Directory.CreateDirectory(directory);
            frame.Save(Path.Combine(directory, name + ".png"));
        }

        window.Hide();
    }

    private static DetailsWindow BuildDetails()
    {
        var now = DateTimeOffset.UtcNow;
        var window = new DetailsWindow();

        window.Render(new UsageState
        {
            Kind = UsageStateKind.Ready,
            Snapshot = new UsageSnapshot
            {
                Session = new UsageWindow(39, now.AddHours(1).AddMinutes(54)),
                Weekly = new UsageWindow(21, now.AddDays(3).AddHours(2)),
                ScopedWeekly = [new ScopedUsageWindow("Fable", new UsageWindow(2, now.AddDays(3)))],
                ExtraUsage = new ExtraUsage(
                    IsEnabled: true, Used: 22.76m, Limit: 50m, Utilization: 46d,
                    Currency: "EUR", Decimals: 2),
                RetrievedAt = now,
                TokenSource = TokenSource.OAuth
            }
        });

        return window;
    }

    private static ReleaseNotesWindow BuildReleaseNotes()
    {
        var window = new ReleaseNotesWindow();
        window.Render(ChangelogResource.All(), new Version(0, 5, 0), ChangelogResource.IsTranslated);

        return window;
    }

    private sealed class TemporaryFile : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(), $"cuc-render-{Guid.NewGuid():N}.json");

        public void Dispose()
        {
            if (File.Exists(Path))
            {
                File.Delete(Path);
            }
        }
    }
}
