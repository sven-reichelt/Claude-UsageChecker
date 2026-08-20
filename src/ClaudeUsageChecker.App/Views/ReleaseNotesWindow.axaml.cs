using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using ClaudeUsageChecker.App.Services;
using ClaudeUsageChecker.Core.Localization;
using ClaudeUsageChecker.Core.Release;

namespace ClaudeUsageChecker.App.Views;

/// <summary>
/// Shows what has changed since the version that ran last.
/// </summary>
/// <remarks>
/// Appears once after an update. Anyone who merely wants to know what an update
/// brought should have to visit neither the release page nor the repository for
/// it.
/// </remarks>
public partial class ReleaseNotesWindow : Window
{
    public ReleaseNotesWindow()
    {
        InitializeComponent();

        // Longer translations grow the window downwards; without this it
        // can end up reaching past the bottom edge of the screen.
        Opened += (_, _) => ScreenFit.Apply(this, ContentScroller);

        Title = T.NotesTitle;
        CloseButton.Content = T.Close;

        CloseButton.Click += (_, _) => Close();
    }

    /// <summary>
    /// Renders the versions handed in, newest first.
    /// </summary>
    /// <param name="releases">The versions to show.</param>
    /// <param name="previous">
    /// The version that ran before. For the heading only - with exactly one new
    /// version its number appears there, otherwise the span.
    /// </param>
    /// <param name="translated">
    /// Whether the changelog exists in the language that is set. Otherwise it is
    /// shown in English, and the window says so.
    /// </param>
    public void Render(
        IReadOnlyList<ReleaseNotes> releases, ProgramVersion? previous = null, bool translated = true)
    {
        ArgumentNullException.ThrowIfNull(releases);

        NotesPanel.Children.Clear();

        TranslationNotice.IsVisible = !translated;
        TranslationNotice.Text = translated ? null : T.NotesTranslationMissing;

        if (releases.Count == 0)
        {
            HeadlineText.Text = T.NotesNone;
            SubtitleText.Text = T.NotesNoneHint;
            SubtitleText.IsVisible = true;
            return;
        }

        HeadlineText.Text = releases.Count == 1
            ? T.NotesHeading(Display(releases[0].Version))
            : T.NotesHeadingMultiple(Display(releases[0].Version), releases.Count - 1);

        // With its label, where there is one: "previously ran 0.7.1-beta.5"
        // tells a tester something that "0.7.1" would hide.
        SubtitleText.Text = previous is null ? null : T.NotesPrevious(previous.ToString());
        SubtitleText.IsVisible = SubtitleText.Text is not null;

        foreach (var release in releases)
        {
            NotesPanel.Children.Add(BuildRelease(release, withHeading: releases.Count > 1));
        }
    }

    /// <summary>Builds the block of one version.</summary>
    /// <param name="withHeading">
    /// With a single version the number is already in the window heading - a
    /// second one next to it would be duplication.
    /// </param>
    private static Control BuildRelease(ReleaseNotes release, bool withHeading)
    {
        var panel = new StackPanel { Spacing = 10 };

        if (withHeading)
        {
            panel.Children.Add(new TextBlock
            {
                Text = release.Date is { } datum
                    ? T.NotesReleaseDated(
                        Display(release.Version), datum.ToString("d", CultureInfo.CurrentCulture))
                    : T.NotesRelease(Display(release.Version)),
                FontWeight = FontWeight.SemiBold,
                FontSize = 13
            });
        }

        foreach (var section in release.Sections)
        {
            panel.Children.Add(BuildSection(section));
        }

        return panel;
    }

    private static Control BuildSection(ReleaseNoteSection section)
    {
        var panel = new StackPanel { Spacing = 6 };

        if (!string.IsNullOrEmpty(section.Title))
        {
            panel.Children.Add(new TextBlock
            {
                Text = section.Title,
                FontWeight = FontWeight.SemiBold,
                FontSize = 12,
                Opacity = 0.85
            });
        }

        foreach (var entry in section.Entries)
        {
            panel.Children.Add(BuildEntry(entry));
        }

        return panel;
    }

    /// <summary>
    /// One bullet point: bullet and text side by side, so that wrapped text stays
    /// under itself instead of sliding under the bullet. A follow-up paragraph
    /// gets no second bullet.
    /// </summary>
    private static Control BuildEntry(ReleaseNoteEntry entry)
    {
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("14,*"),
            Margin = new Avalonia.Thickness(entry.IsContinuation ? 14 : 0, 0, 0, 0)
        };

        var bullet = new TextBlock
        {
            Text = entry.IsContinuation ? string.Empty : "•",
            FontSize = 12,
            VerticalAlignment = VerticalAlignment.Top
        };

        var text = new TextBlock
        {
            Text = entry.Text,
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
            Opacity = entry.IsContinuation ? 0.85 : 1d
        };

        Grid.SetColumn(text, 1);
        grid.Children.Add(bullet);
        grid.Children.Add(text);

        return grid;
    }

    private static string Display(Version version) =>
        version.Build >= 0 ? version.ToString(3) : version.ToString();
}
