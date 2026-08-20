using System;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.Controls;
using ClaudeUsageChecker.App.Services;
using ClaudeUsageChecker.App.Settings;
using ClaudeUsageChecker.Core.Authentication;
using ClaudeUsageChecker.Core.Localization;
using ClaudeUsageChecker.Core.Authentication.OAuth;
using ClaudeUsageChecker.Core.Platform;

namespace ClaudeUsageChecker.App.Views;

/// <summary>
/// Settings. Holds no token: the application reads the one belonging to the
/// Claude Code installation, or signs in on its own - see docs/api-research.md.
/// </summary>
public partial class SettingsWindow : Window
{
    private readonly SettingsStore _settingsStore;
    private readonly OAuthTokenStore? _oauthTokenStore;
    private readonly Func<InstallResult>? _relocate;
    private readonly Action<bool> _applyAutostart;
    private AppSettings _settings;
    private int _versionClicks;

    /// <summary>How often the version number has to be clicked to reveal the channel.</summary>
    internal const int ClicksToRevealChannel = 5;

    public SettingsWindow() : this(new SettingsStore(), new AppSettings())
    {
    }

    public SettingsWindow(
        SettingsStore settingsStore,
        AppSettings settings,
        OAuthTokenStore? oauthTokenStore = null,
        Func<InstallResult>? relocate = null,
        Action<bool>? applyAutostart = null)
    {
        _settingsStore = settingsStore;
        _settings = settings;
        _oauthTokenStore = oauthTokenStore;
        _relocate = relocate;

        // Injectable purely for the tests: the real route writes to the Run key
        // of the registry. A test that presses "save" would otherwise delete the
        // autostart entry of the user on whose machine it happens to run.
        _applyAutostart = applyAutostart ?? (enabled => AutostartManager.Apply(enabled));

        InitializeComponent();

        // The picker is filled before the labelling: it populates the field whose
        // content is not touched afterwards.
        LanguageBox.ItemsSource = Language.All.Select(l => l.NativeName).ToList();
        LanguageBox.SelectedIndex = Language.All.ToList()
            .FindIndex(l => l.Code == (Language.Find(settings.Language) ?? Localizer.Current.Language).Code);

        ApplyTexts();

        IntervalBox.Value = settings.PollIntervalSeconds;
        LaunchAtLoginBox.IsChecked = settings.LaunchAtLogin;
        CheckUpdatesBox.IsChecked = settings.CheckForUpdates;
        WarningThresholdBox.Value = (decimal)settings.WarningThreshold;
        CriticalThresholdBox.Value = (decimal)settings.CriticalThreshold;

        VersionText.Text = ProgramVersion.Current.IsPreRelease
            ? T.VersionPreRelease(ProgramVersion.Current.ToString())
            : T.Version(ProgramVersion.Current.ToString());

        ChannelBox.ItemsSource = new List<string> { T.SettingsChannelStable, T.SettingsChannelPreRelease };
        ChannelBox.SelectedIndex = settings.Channel == UpdateChannel.PreRelease ? 1 : 0;

        // Visible from the start exactly while a pre-release is selected: a
        // setting that could not be undone without knowing the trick would be a
        // trap. Back on the published releases it disappears again with the next
        // opening - it is not a switch anyone needs in view.
        ChannelSection.IsVisible = settings.Channel == UpdateChannel.PreRelease;

        VersionText.PointerPressed += (_, _) => CountVersionClick();

        SignInButton.Click += (_, _) => SignInRequested?.Invoke(this, EventArgs.Empty);
        SignOutButton.Click += (_, _) => SignOut();
        SaveButton.Click += (_, _) => SaveAndClose();
        CancelButton.Click += (_, _) => Close();

        LaunchAtLoginBox.IsCheckedChanged += (_, _) => UpdateRelocationHint();
        Opened += (_, _) => LimitToScreen();

        UpdateSignInStatus();
        UpdateRelocationHint();
    }

    /// <summary>
    /// Counts the clicks on the version number and reveals the choice of update
    /// channel at the fifth.
    /// </summary>
    /// <remarks>
    /// Five is enough that nobody arrives there by accident and few enough to be
    /// found on purpose. Once revealed it stays revealed - it is saved along
    /// with everything else, so nobody has to hunt for it twice.
    /// </remarks>
    private void CountVersionClick()
    {
        if (ChannelSection.IsVisible)
        {
            return;
        }

        _versionClicks++;

        if (_versionClicks >= ClicksToRevealChannel)
        {
            ChannelSection.IsVisible = true;
            ContentScroller.ScrollToEnd();
        }
    }

    /// <summary>Sets every fixed label from the language file.</summary>
    private void ApplyTexts()
    {
        Title = T.SettingsTitle;

        SignInHeading.Text = T.SettingsSignInSection;
        SignInButton.Content = T.SettingsSignIn;
        SignOutButton.Content = T.SettingsSignOut;


        BehaviourHeading.Text = T.SettingsBehaviourSection;
        IntervalLabel.Text = T.SettingsInterval;
        LaunchAtLoginBox.Content = T.SettingsLaunchAtLogin;
        CheckUpdatesBox.Content = T.SettingsCheckForUpdates;

        LanguageHeading.Text = T.SettingsLanguageSection;
        LanguageHint.Text = T.SettingsLanguageHint;
        LanguageLabel.Text = T.SettingsLanguageLabel;

        ChannelHeading.Text = T.SettingsChannelSection;
        ChannelHint.Text = T.SettingsChannelHint;
        ChannelLabel.Text = T.SettingsChannelLabel;

        ThresholdHeading.Text = T.SettingsThresholdSection;
        ThresholdIntro.Text = T.SettingsThresholdHint;
        WarningLabel.Text = T.SettingsWarningThreshold;
        CriticalLabel.Text = T.SettingsCriticalThreshold;

        CancelButton.Content = T.Cancel;
        SaveButton.Content = T.Save;
    }

    /// <summary>The language currently selected in the picker.</summary>
    private Language SelectedLanguage =>
        LanguageBox.SelectedIndex >= 0 && LanguageBox.SelectedIndex < Language.All.Count
            ? Language.All[LanguageBox.SelectedIndex]
            : Localizer.Current.Language;

    /// <summary>The user wants to start the sign-in.</summary>
    public event EventHandler? SignInRequested;

    /// <summary>
    /// Keeps the window inside the working area of the screen it appears on.
    /// </summary>
    /// <remarks>
    /// The window grows with its content and cannot be resized. On a low screen
    /// it therefore extended past the bottom edge - taking the "save" button
    /// with it. The screen is only known once the window is open, hence here
    /// rather than in the constructor. The arithmetic sits in
    /// <see cref="ScreenFit"/>, because every window here has the same problem.
    /// </remarks>
    private void LimitToScreen() => ScreenFit.Apply(this, ContentScroller);

    /// <summary>Refreshes the sign-in display from outside.</summary>
    public void RefreshSignInStatus() => UpdateSignInStatus();

    private void UpdateSignInStatus()
    {
        if (_oauthTokenStore is not { IsSupported: true })
        {
            SignInStatus.Text = T.NoSecureStore;
            SignInButton.IsEnabled = false;
            SignOutButton.IsEnabled = false;
            return;
        }

        var tokens = _oauthTokenStore.Read();
        SignInStatus.Text = tokens is null
            ? T.SettingsNotSignedIn
            : T.SettingsSignedIn(
                tokens.Scope ?? T.Unknown, tokens.ExpiresAt?.ToLocalTime() ?? default);

        SignOutButton.IsEnabled = tokens is not null;
    }

    private void SignOut()
    {
        if (_oauthTokenStore is null)
        {
            return;
        }

        try
        {
            _oauthTokenStore.Clear();
            UpdateSignInStatus();
            SettingsChanged?.Invoke(this, _settings);
        }
        catch (InvalidOperationException ex)
        {
            SignInStatus.Text = ex.Message;
        }
    }

    /// <summary>Raised when settings or the token have changed.</summary>
    public event EventHandler<AppSettings>? SettingsChanged;

    /// <summary>
    /// Points out that autostart entails moving the application.
    /// </summary>
    /// <remarks>
    /// An autostart entry pointing into the downloads folder breaks the first
    /// time that folder is cleaned out. Ticking the box therefore moves the
    /// application to its permanent location as well - but nobody should be
    /// surprised by that.
    /// </remarks>
    private void UpdateRelocationHint()
    {
        var noetig = (LaunchAtLoginBox.IsChecked ?? false) && _relocate is not null && SelfInstaller.ShouldOffer;

        RelocationHint.IsVisible = noetig;
        RelocationHint.Text = noetig
            ? T.SettingsRelocationHint(SelfInstaller.TargetPath)
            : null;
    }

    private void SaveAndClose()
    {
        var warning = (double)(WarningThresholdBox.Value ?? 75m);
        var critical = (double)(CriticalThresholdBox.Value ?? 90m);

        // A warning threshold above the critical one would never take effect.
        // Rather than quietly correcting it, the window stays open and says what
        // is wrong - otherwise something else would end up there than was typed.
        if (AppSettings.ValidateThresholds(warning, critical) is { } problem)
        {
            ThresholdHint.Text = problem;
            ThresholdHint.IsVisible = true;
            return;
        }

        ThresholdHint.IsVisible = false;

        var language = SelectedLanguage;

        _settings = new AppSettings
        {
            PollIntervalSeconds = (int)(IntervalBox.Value ?? 300),
            LaunchAtLogin = LaunchAtLoginBox.IsChecked ?? false,
            CheckForUpdates = CheckUpdatesBox.IsChecked ?? true,
            WarningThreshold = warning,
            CriticalThreshold = critical,
            Language = language.Code,
            InstallPromptShown = _settings.InstallPromptShown,
            LastRunVersion = _settings.LastRunVersion,
            Channel = ChannelBox.SelectedIndex == 1 ? UpdateChannel.PreRelease : UpdateChannel.Stable
        };

        _settingsStore.Save(_settings);

        // Switch before reporting: whoever reacts to the change - the context
        // menu, say - should already find the new texts in place.
        if (language.Code != Localizer.Current.Language.Code)
        {
            Localizer.Use(language);
        }

        // Autostart needs a permanent location. Unticking, by contrast, only
        // removes the entry - an application once installed stays where it is.
        if (_settings.LaunchAtLogin && _relocate is not null && SelfInstaller.ShouldOffer)
        {
            SaveButton.IsEnabled = false;
            RelocationHint.IsVisible = true;
            RelocationHint.Text = T.SettingsRelocating;

            var result = _relocate();
            if (!result.Succeeded)
            {
                RelocationHint.Text = result.Message;
                SaveButton.IsEnabled = true;
                return;
            }

            RelocationHint.Text = result.Message;
            SettingsChanged?.Invoke(this, _settings);
            Close();
            return;
        }

        _applyAutostart(_settings.LaunchAtLogin);
        SettingsChanged?.Invoke(this, _settings);
        Close();
    }
}
