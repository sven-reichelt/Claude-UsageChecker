using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
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

        CopyAddressButton.Click += (_, _) => CopyAddress();
        OpenBrowserButton.Click += (_, _) => StartSignIn();
        CompleteButton.Click += (_, _) => CompleteSignIn();
        CloseButton.Click += (_, _) => Close();

        // Bubbling and explicitly also for handled events: a TextBox handles
        // the press in its own template and puts the caret where the pointer
        // was, which undoes a selection made before that. So this has to run
        // after it, not before - a tunnelling handler had its SelectAll
        // wiped out again.
        UrlBox.AddHandler(
            InputElement.PointerPressedEvent,
            (_, _) => CopyWholeAddress(),
            RoutingStrategies.Bubble,
            handledEventsToo: true);

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
        CopyAddressButton.Content = T.SignInCopyAddress;
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

    /// <summary>
    /// Makes sure there is an authorization request and returns its address,
    /// or <c>null</c> if signing in is not available here.
    /// </summary>
    /// <remarks>
    /// An existing request is reused rather than replaced. Every request
    /// carries its own code verifier, and only the last one created can be
    /// redeemed - so creating a second one would quietly invalidate the
    /// address that was just copied or just opened in a browser.
    /// </remarks>
    private string? PrepareRequest()
    {
        if (_oauthClient is null)
        {
            StatusText.Text = T.SignInUnavailable;
            return null;
        }

        _pendingRequest ??= _oauthClient.CreateAuthorizationRequest();

        var address = _pendingRequest.Url.ToString();
        UrlBox.Text = address;
        CompleteButton.IsEnabled = true;

        return address;
    }

    private void StartSignIn()
    {
        if (PrepareRequest() is not { } address)
        {
            return;
        }

        StatusText.Text = T.SignInGrantInBrowser;

        try
        {
            Process.Start(new ProcessStartInfo(address) { UseShellExecute = true });
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            StatusText.Text = T.SignInBrowserFailed;
        }
    }

    /// <summary>
    /// The second way through step 1: hand over the address and leave the
    /// browser alone - for a different browser, another profile or another
    /// device.
    /// </summary>
    private async void CopyAddress()
    {
        if (PrepareRequest() is not { } address)
        {
            return;
        }

        StatusText.Text = await CopyToClipboard(address).ConfigureAwait(true)
            ? T.SignInAddressCopied
            : T.SignInCopyFailed;
    }

    /// <summary>Copies the whole address when the field is clicked, not the part under the pointer.</summary>
    private async void CopyWholeAddress()
    {
        var address = UrlBox.Text;
        if (string.IsNullOrEmpty(address))
        {
            return;
        }

        // Selecting all of it is the visible part of the answer: it shows
        // what went to the clipboard.
        UrlBox.SelectAll();

        StatusText.Text = await CopyToClipboard(address).ConfigureAwait(true)
            ? T.SignInAddressCopied
            : T.SignInCopyFailed;
    }

    /// <summary>
    /// Writes text to the clipboard and says whether it worked.
    /// </summary>
    /// <remarks>
    /// Both callers are event handlers, and an exception out of an
    /// <c>async void</c> has no one to catch it - so nothing is allowed to
    /// escape here. There is no clipboard at all on some platforms and in
    /// tests.
    /// </remarks>
    private async Task<bool> CopyToClipboard(string text)
    {
        try
        {
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard is null)
            {
                return false;
            }

            await clipboard.SetTextAsync(text).ConfigureAwait(true);
            return true;
        }
        catch (Exception ex)
        {
            CrashReporter.Write(ex, "Copying the sign-in address to the clipboard");
            return false;
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
