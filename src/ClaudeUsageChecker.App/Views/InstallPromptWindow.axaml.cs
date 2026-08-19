using System;
using System.Linq;
using Avalonia.Controls;
using ClaudeUsageChecker.App.Services;
using ClaudeUsageChecker.Core.Localization;

namespace ClaudeUsageChecker.App.Views;

/// <summary>
/// Asks on the first start whether the application should set itself up
/// permanently - and lets the language be chosen at the same time.
/// </summary>
/// <remarks>
/// <para>
/// The question is asked exactly once. Anyone who declines does not want to be
/// asked again on every start - and anyone who changes their mind finds the way
/// in the settings.
/// </para>
/// <para>
/// The language picker sits here because this is the first and often the only
/// window a new user gets to see. It applies regardless of the answer:
/// <b>both</b> buttons adopt it, because someone who changes the language and
/// then picks "do not set up" still wanted the change.
/// </para>
/// </remarks>
public partial class InstallPromptWindow : Window
{
    public InstallPromptWindow()
    {
        InitializeComponent();

        // Longer translations grow the window downwards; without this it
        // can end up reaching past the bottom edge of the screen.
        Opened += (_, _) => ScreenFit.Apply(this);

        LanguageBox.ItemsSource = Language.All.Select(l => l.NativeName).ToList();
        LanguageBox.SelectedIndex = Language.All.ToList()
            .FindIndex(l => l.Code == Localizer.Current.Language.Code);

        ApplyTexts();

        // Switch at once: a language choice that only takes effect after the
        // click leaves the user unsure whether they picked the right one.
        LanguageBox.SelectionChanged += (_, _) =>
        {
            Localizer.Use(SelectedLanguage);
            ApplyTexts();
            LanguageChanged?.Invoke(this, SelectedLanguage);
        };

        InstallButton.Click += (_, _) => Install();
        LaterButton.Click += (_, _) =>
        {
            Declined?.Invoke(this, EventArgs.Empty);
            Close();
        };
    }

    /// <summary>The setup succeeded; the new instance is already running.</summary>
    public event EventHandler? Installed;

    /// <summary>The user does not want to set up.</summary>
    public event EventHandler? Declined;

    /// <summary>The user picked a different language.</summary>
    public event EventHandler<Language>? LanguageChanged;

    /// <summary>The language currently selected in the picker.</summary>
    public Language SelectedLanguage =>
        LanguageBox.SelectedIndex >= 0 && LanguageBox.SelectedIndex < Language.All.Count
            ? Language.All[LanguageBox.SelectedIndex]
            : Localizer.Current.Language;

    /// <summary>Sets every fixed label from the language file.</summary>
    private void ApplyTexts()
    {
        Title = T.InstallTitle;
        HeadingText.Text = T.InstallHeading;
        LanguageLabel.Text = T.InstallLanguage;
        LanguageHint.Text = T.InstallLanguageHint;
        IntroText.Text = T.InstallIntro;
        ListIntroText.Text = T.InstallListIntro;
        BulletCopyText.Text = T.InstallBulletCopy;
        BulletAutostartText.Text = T.InstallBulletAutostart;
        BulletRestartText.Text = T.InstallBulletRestart;
        OutroText.Text = T.InstallOutro;
        InstallButton.Content = T.InstallAccept;
        LaterButton.Content = T.InstallDecline;

        TargetText.Text = SelfInstaller.TargetPath;
    }

    private void Install()
    {
        InstallButton.IsEnabled = false;
        StatusText.Text = T.InstallRunning;
        StatusText.IsVisible = true;

        var result = SelfInstaller.Install();

        if (!result.Succeeded)
        {
            StatusText.Text = result.Message;
            InstallButton.IsEnabled = true;
            return;
        }

        StatusText.Text = result.Message;
        Installed?.Invoke(this, EventArgs.Empty);
    }
}
