using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia.Controls;
using ClaudeUsageChecker.Core.Authentication.OAuth;
using ClaudeUsageChecker.Core.Localization;

namespace ClaudeUsageChecker.App.Views;

/// <summary>
/// Guides through the application's own sign-in: open the page, paste the code,
/// done.
/// </summary>
/// <remarks>
/// Pasting by hand rather than a local web server, on purpose: the application
/// needs to open no port and run no network service for it - one attack surface
/// less on the user's machine.
/// </remarks>
public partial class SignInWindow : Window
{
    private readonly AnthropicOAuthClient? _oauthClient;
    private readonly OAuthTokenStore? _tokenStore;

    private AuthorizationRequest? _pendingRequest;

    public SignInWindow() : this(null, null)
    {
    }

    public SignInWindow(AnthropicOAuthClient? oauthClient, OAuthTokenStore? tokenStore)
    {
        _oauthClient = oauthClient;
        _tokenStore = tokenStore;

        InitializeComponent();

        // Longer translations grow the window downwards; without this it
        // can end up reaching past the bottom edge of the screen.
        Opened += (_, _) => ScreenFit.Apply(this);
        ApplyTexts();

        OpenBrowserButton.Click += (_, _) => StartSignIn();
        CompleteButton.Click += (_, _) => CompleteSignIn();
        CloseButton.Click += (_, _) => Close();

        CompleteButton.IsEnabled = false;
        UpdateSignedInState();
    }

    /// <summary>Sets every fixed label from the language file.</summary>
    private void ApplyTexts()
    {
        Title = T.SignInTitle;
        Heading.Text = T.SignInHeading;
        Intro.Text = T.SignInIntro;
        Step1.Text = T.SignInStep1;
        OpenBrowserButton.Content = T.SignInOpenBrowser;
        UrlHint.Text = T.SignInUrlHint;
        Step2.Text = T.SignInStep2;
        CodeBox.Watermark = T.SignInCodeWatermark;
        CompleteButton.Content = T.SignInComplete;
        CloseButton.Content = T.Close;
    }

    /// <summary>Raised when a sign-in has been completed successfully.</summary>
    public event EventHandler? SignedIn;

    private void UpdateSignedInState()
    {
        var vorhanden = _tokenStore?.Read();
        SignedInText.Text = vorhanden is null
            ? T.SignInNotYet
            : T.SignInSignedIn(
                vorhanden.ExpiresAt?.ToLocalTime() ?? default, vorhanden.Scope ?? T.Unknown);
    }

    private void StartSignIn()
    {
        if (_oauthClient is null)
        {
            StatusText.Text = T.SignInUnavailable;
            return;
        }

        _pendingRequest = _oauthClient.CreateAuthorizationRequest();
        UrlBox.Text = _pendingRequest.Url.ToString();
        CompleteButton.IsEnabled = true;
        StatusText.Text = T.SignInGrantInBrowser;

        try
        {
            Process.Start(new ProcessStartInfo(_pendingRequest.Url.ToString()) { UseShellExecute = true });
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            StatusText.Text = T.SignInBrowserFailed;
        }
    }

    private async void CompleteSignIn()
    {
        if (_oauthClient is null || _tokenStore is null)
        {
            return;
        }

        if (_pendingRequest is null)
        {
            StatusText.Text = T.SignInOpenPageFirst;
            return;
        }

        var code = CodeBox.Text?.Trim();
        if (string.IsNullOrEmpty(code))
        {
            StatusText.Text = T.SignInPasteCode;
            return;
        }

        CompleteButton.IsEnabled = false;
        StatusText.Text = T.SignInRedeeming;

        try
        {
            var tokens = await _oauthClient.ExchangeCodeAsync(code, _pendingRequest).ConfigureAwait(true);
            _tokenStore.Write(tokens);

            CodeBox.Text = string.Empty;
            _pendingRequest = null;
            StatusText.Text = T.SignInSuccess;
            UpdateSignedInState();
            SignedIn?.Invoke(this, EventArgs.Empty);
        }
        catch (OAuthException ex)
        {
            StatusText.Text = ex.Message;
            CompleteButton.IsEnabled = true;
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            StatusText.Text = T.SignInSaveFailed(ex.Message);
            CompleteButton.IsEnabled = true;
        }
    }
}
