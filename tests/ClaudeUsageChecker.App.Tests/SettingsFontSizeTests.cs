using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ClaudeUsageChecker.App.Settings;
using ClaudeUsageChecker.App.Views;
using ClaudeUsageChecker.Core.Authentication;
using ClaudeUsageChecker.Core.Models;

namespace ClaudeUsageChecker.App.Tests;

/// <summary>
/// The settings are set in small type, and in one size of it.
/// </summary>
/// <remarks>
/// Labels and hints were set by hand while switches, number fields, pickers and
/// buttons took the theme's 14 because nobody had set them - so the window mixed
/// two sizes by accident. Now: 12 for the headings and for "Cancel" and "Save",
/// 10 for everything else.
/// </remarks>
public class SettingsFontSizeTests
{
    private const double HeadingSize = 12;
    private const double BodySize = 10;
    private const double ButtonSize = 12;
    private const double MaximumFieldHeight = 26;

    /// <summary>The two buttons the whole window leads up to.</summary>
    private static readonly string[] Answers = ["CancelButton", "SaveButton"];

    [AvaloniaFact]
    public void TheSettingsSpeakInOneSize()
    {
        var path = Path.Combine(Path.GetTempPath(), $"cuc-fonts-{Guid.NewGuid():N}.json");

        try
        {
            var window = new SettingsWindow(
                new SettingsStore(path),
                new AppSettings { Channel = UpdateChannel.PreRelease },
                applyAutostart: _ => { },
                readClaudeCodeToken: () => Task.FromResult<AccessToken?>(null),
                plan: new SubscriptionPlan("claude_max", "default_claude_max_5x"));

            window.Show();

            // Templates are applied during layout; before it, the switch labels
            // and the text inside the number fields do not exist yet.
            Dispatcher.UIThread.RunJobs();

            var answers = Answers
                .SelectMany(name => window.FindControl<Button>(name)!.GetVisualDescendants())
                .ToHashSet();

            var texts = window.GetVisualDescendants()
                .Select(v => Describe(v, answers.Contains(v)))
                .Where(t => t is { Visible: true, Content.Length: > 0 })
                .ToList();

            window.Close();

            // Otherwise the check would pass on a window that drew nothing.
            Assert.Contains(texts, t => t.Kind == nameof(TextPresenter));
            Assert.True(texts.Count > 40, $"Only {texts.Count} texts found - did the templates apply?");
            Assert.Equal(2, texts.Count(t => t.IsAnswer));
            Assert.True(texts.Count(t => t.IsHeading) >= 8, "The headings were not found.");

            var wrong = texts
                .Where(IsWrong)
                .Select(t => $"{t.Kind} \"{t.Content}\" at {t.Size}")
                .ToList();

            Assert.True(wrong.Count == 0, string.Join(" | ", wrong));
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// The fields and switches are sized to the type they hold.
    /// </summary>
    /// <remarks>
    /// The theme builds number fields, pickers and check boxes 32 high, for text
    /// in 14. A number field is three controls deep and each of them carries that
    /// minimum on its own, so a style that reaches only the outer one changes
    /// nothing you can see - which is how the first attempt went.
    /// </remarks>
    [AvaloniaFact]
    public void TheFieldsAndSwitchesFitTheirType()
    {
        var path = Path.Combine(Path.GetTempPath(), $"cuc-fields-{Guid.NewGuid():N}.json");

        try
        {
            var window = new SettingsWindow(
                new SettingsStore(path), new AppSettings(),
                applyAutostart: _ => { },
                readClaudeCodeToken: () => Task.FromResult<AccessToken?>(null));

            window.Show();
            Dispatcher.UIThread.RunJobs();

            var controls = window.GetVisualDescendants()
                .OfType<Control>()
                .Where(c => c is NumericUpDown or ComboBox || c is CheckBox box && box.Classes.Contains("switch"))
                .Where(c => c.IsEffectivelyVisible)
                .ToList();

            window.Close();

            Assert.True(controls.Count >= 12, $"Only {controls.Count} fields and switches found.");

            var tall = controls
                .Where(c => c.Bounds.Height > Allowed(c))
                .Select(c => $"{c.Name ?? c.GetType().Name} at {c.Bounds.Height:0}")
                .ToList();

            Assert.True(tall.Count == 0, $"Taller than {MaximumFieldHeight}: {string.Join(" | ", tall)}");
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// A switch whose label folds onto two lines is rightly taller; what must
    /// not happen is a row taller than both its switch and its label.
    /// </summary>
    private static double Allowed(Control control) =>
        control is CheckBox
            ? Math.Max(MaximumFieldHeight, control.GetVisualDescendants()
                .OfType<ContentPresenter>()
                .Where(p => p.Name == "PART_ContentPresenter")
                .Select(p => p.Bounds.Height)
                .DefaultIfEmpty(0)
                .Max())
            : MaximumFieldHeight;

    /// <summary>
    /// The two answers and the headings are held to their size exactly. The body
    /// may go smaller - a hint set smaller still is not what this is about - but
    /// never larger.
    /// </summary>
    private static bool IsWrong(Text text) =>
        text.IsAnswer ? text.Size != ButtonSize
        : text.IsHeading ? text.Size != HeadingSize
        : text.Size > BodySize;

    private sealed record Text(string Kind, string? Content, double Size, bool IsHeading, bool Visible, bool IsAnswer);

    private static Text Describe(Visual visual, bool isAnswer) => visual switch
    {
        TextBlock block => new(
            nameof(TextBlock), block.Text, block.FontSize, block.FontWeight >= FontWeight.SemiBold,
            block.IsEffectivelyVisible, isAnswer),
        TextPresenter presenter => new(
            nameof(TextPresenter), presenter.Text, presenter.FontSize, presenter.FontWeight >= FontWeight.SemiBold,
            presenter.IsEffectivelyVisible, isAnswer),
        _ => new(visual.GetType().Name, null, 0, false, false, false)
    };
}
