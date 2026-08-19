using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia.Controls;
using ClaudeUsageChecker.Core.Authentication.OAuth;

namespace ClaudeUsageChecker.App.Views;

/// <summary>
/// Fuehrt durch die eigene Anmeldung: Seite oeffnen, Code einfuegen, fertig.
/// </summary>
/// <remarks>
/// Bewusst mit Einfuegen von Hand statt mit einem lokalen Webserver: Die
/// Anwendung muss dafuer keinen Port oeffnen und keinen Netzwerkdienst
/// betreiben - eine Angriffsflaeche weniger auf dem Rechner des Nutzers.
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

        OpenBrowserButton.Click += (_, _) => StartSignIn();
        CompleteButton.Click += (_, _) => CompleteSignIn();
        CloseButton.Click += (_, _) => Close();

        CompleteButton.IsEnabled = false;
        UpdateSignedInState();
    }

    /// <summary>Wird ausgeloest, wenn eine Anmeldung erfolgreich abgeschlossen wurde.</summary>
    public event EventHandler? SignedIn;

    private void UpdateSignedInState()
    {
        var vorhanden = _tokenStore?.Read();
        SignedInText.Text = vorhanden is null
            ? "Noch nicht angemeldet."
            : $"Angemeldet. Gueltig bis {vorhanden.ExpiresAt?.ToLocalTime():g}, Rechte: {vorhanden.Scope ?? "unbekannt"}.";
    }

    private void StartSignIn()
    {
        if (_oauthClient is null)
        {
            StatusText.Text = "Die Anmeldung ist in dieser Ansicht nicht verfuegbar.";
            return;
        }

        _pendingRequest = _oauthClient.CreateAuthorizationRequest();
        UrlBox.Text = _pendingRequest.Url.ToString();
        CompleteButton.IsEnabled = true;
        StatusText.Text = "Bitte im Browser die Freigabe erteilen und den angezeigten Code hier einfuegen.";

        try
        {
            Process.Start(new ProcessStartInfo(_pendingRequest.Url.ToString()) { UseShellExecute = true });
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            StatusText.Text = "Der Browser liess sich nicht oeffnen. Bitte die Adresse oben kopieren.";
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
            StatusText.Text = "Bitte zuerst die Anmeldeseite oeffnen.";
            return;
        }

        var code = CodeBox.Text?.Trim();
        if (string.IsNullOrEmpty(code))
        {
            StatusText.Text = "Bitte den Code von der Anthropic-Seite einfuegen.";
            return;
        }

        CompleteButton.IsEnabled = false;
        StatusText.Text = "Code wird eingeloest ...";

        try
        {
            var tokens = await _oauthClient.ExchangeCodeAsync(code, _pendingRequest).ConfigureAwait(true);
            _tokenStore.Write(tokens);

            CodeBox.Text = string.Empty;
            _pendingRequest = null;
            StatusText.Text = "Anmeldung erfolgreich. Die Anwendung nutzt ab sofort ihr eigenes Zugriffsrecht.";
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
            StatusText.Text = "Die Anmeldung konnte nicht gespeichert werden: " + ex.Message;
            CompleteButton.IsEnabled = true;
        }
    }
}
