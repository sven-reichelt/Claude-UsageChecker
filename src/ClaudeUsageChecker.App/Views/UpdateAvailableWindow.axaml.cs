using System;
using Avalonia.Controls;
using ClaudeUsageChecker.App.Services;
using ClaudeUsageChecker.Core.Localization;

namespace ClaudeUsageChecker.App.Views;

/// <summary>
/// Asks, once per version, whether a newer version found in the background is
/// to be installed now or mentioned again tomorrow.
/// </summary>
/// <remarks>
/// <para>
/// The check in the background never installs by itself - see
/// <see cref="UpdatePolicy"/>. So it asks, and the answer is one of two
/// buttons. Whoever lets it be until tomorrow and restarts the machine in the
/// meantime gets the update at the next start, where automatic updates are on.
/// </para>
/// <para>
/// Where this copy cannot replace itself - a development build, or a Mac whose
/// applications folder it may not write to - the first button leads to the
/// release page instead. A button that cannot do what it says is worse than
/// one that says less.
/// </para>
/// </remarks>
public partial class UpdateAvailableWindow : Window
{
    private readonly UpdateCheckResult _update;
    private bool _decided;

    public UpdateAvailableWindow() : this(
        new UpdateCheckResult { Status = UpdateCheckStatus.UpdateAvailable }, canInstall: true)
    {
    }

    /// <param name="update">The version that was found.</param>
    /// <param name="canInstall">Whether this copy can install it by itself.</param>
    public UpdateAvailableWindow(UpdateCheckResult update, bool canInstall)
    {
        _update = update ?? throw new ArgumentNullException(nameof(update));
        CanInstall = canInstall && update.CanInstall;

        InitializeComponent();

        Opened += (_, _) => ScreenFit.Apply(this);

        InstallButton.IsVisible = CanInstall;
        ReleasePageButton.IsVisible = !CanInstall && update.ReleasePage is not null;

        InstallButton.Click += (_, _) =>
        {
            _decided = true;
            InstallRequested?.Invoke(this, EventArgs.Empty);
        };

        ReleasePageButton.Click += (_, _) =>
        {
            if (_update.ReleasePage is { } page)
            {
                _decided = true;
                ReleasePageRequested?.Invoke(this, page);
                Close();
            }
        };

        LaterButton.Click += (_, _) => Close();

        // Closed any other way than by a decision - Alt+F4, say - counts as
        // "tomorrow": asking again in two hours would be nagging, never asking
        // again would be forgetting.
        Closed += (_, _) =>
        {
            if (!_decided)
            {
                RemindLaterRequested?.Invoke(this, EventArgs.Empty);
            }
        };

        ApplyTexts();
    }

    /// <summary>Whether the first button installs, rather than leading to the release page.</summary>
    internal bool CanInstall { get; }

    /// <summary>The user wants the update installed now.</summary>
    public event EventHandler? InstallRequested;

    /// <summary>The user wants to download it by hand.</summary>
    public event EventHandler<Uri>? ReleasePageRequested;

    /// <summary>The user wants to hear about it again tomorrow.</summary>
    public event EventHandler? RemindLaterRequested;

    /// <summary>Sets every fixed label from the language file.</summary>
    public void ApplyTexts()
    {
        Title = T.UpdateNoticeTitle;
        HeadingText.Text = T.UpdateNoticeHeading;
        MessageText.Text = _update.Message;
        HintText.Text = CanInstall ? T.UpdateNoticeRestart : T.UpdateNoticeManual;
        InstallButton.Content = T.UpdateNoticeInstall;
        ReleasePageButton.Content = T.DetailsReleasePage;
        LaterButton.Content = T.UpdateNoticeLater;
    }

    /// <summary>
    /// Reports the progress of the installation. While it runs, neither button
    /// can be pressed; a failure gives them back along with the reason.
    /// </summary>
    public void SetProgress(string message, bool busy)
    {
        StatusText.Text = message;
        StatusText.IsVisible = true;
        InstallButton.IsEnabled = !busy;
        LaterButton.IsEnabled = !busy;

        if (!busy)
        {
            // A failed attempt is not a decision about the version.
            _decided = false;
        }
    }

    /// <summary>
    /// Opens the question in front of whatever the user is doing, without taking
    /// the keyboard away - the same way the usage notice appears.
    /// </summary>
    public void Present()
    {
        Topmost = true;
        Show();
        Topmost = false;
    }
}
