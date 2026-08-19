using System;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.Controls;
using ClaudeUsageChecker.App.Services;
using ClaudeUsageChecker.App.Settings;
using ClaudeUsageChecker.Core.Authentication;
using ClaudeUsageChecker.Core.Authentication.OAuth;
using ClaudeUsageChecker.Core.Platform;

namespace ClaudeUsageChecker.App.Views;

/// <summary>
/// Einstellungen samt Hinterlegung des Zugriffstokens.
/// Der Tokenwert wird nie in die Einstellungsdatei geschrieben, sondern
/// ausschliesslich an den verschluesselten Secret-Store uebergeben.
/// </summary>
public partial class SettingsWindow : Window
{
    private readonly ISecretStore _secretStore;
    private readonly SettingsStore _settingsStore;
    private readonly Func<string, Task<TokenValidationResult>>? _validateToken;
    private readonly OAuthTokenStore? _oauthTokenStore;
    private AppSettings _settings;

    public SettingsWindow() : this(SecretStoreFactory.CreateForCurrentPlatform(), new SettingsStore(), new AppSettings())
    {
    }

    public SettingsWindow(
        ISecretStore secretStore,
        SettingsStore settingsStore,
        AppSettings settings,
        Func<string, Task<TokenValidationResult>>? validateToken = null,
        OAuthTokenStore? oauthTokenStore = null)
    {
        _secretStore = secretStore;
        _settingsStore = settingsStore;
        _settings = settings;
        _validateToken = validateToken;
        _oauthTokenStore = oauthTokenStore;

        InitializeComponent();

        IntervalBox.Value = settings.PollIntervalSeconds;
        LaunchAtLoginBox.IsChecked = settings.LaunchAtLogin;
        CheckUpdatesBox.IsChecked = settings.CheckForUpdates;

        VersionText.Text = "Version "
            + (Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "unbekannt");

        SaveTokenButton.Click += (_, _) => SaveToken();
        DeleteTokenButton.Click += (_, _) => DeleteToken();
        SignInButton.Click += (_, _) => SignInRequested?.Invoke(this, EventArgs.Empty);
        SignOutButton.Click += (_, _) => SignOut();
        SaveButton.Click += (_, _) => SaveAndClose();
        CancelButton.Click += (_, _) => Close();

        UpdateTokenStatus();
        UpdateSignInStatus();
    }

    /// <summary>Der Nutzer moechte die eigene Anmeldung starten.</summary>
    public event EventHandler? SignInRequested;

    /// <summary>Aktualisiert die Anzeige der eigenen Anmeldung von aussen.</summary>
    public void RefreshSignInStatus() => UpdateSignInStatus();

    private void UpdateSignInStatus()
    {
        if (_oauthTokenStore is not { IsSupported: true })
        {
            SignInStatus.Text = "Auf diesem System steht kein sicherer Speicher zur Verfuegung.";
            SignInButton.IsEnabled = false;
            SignOutButton.IsEnabled = false;
            return;
        }

        var tokens = _oauthTokenStore.Read();
        SignInStatus.Text = tokens is null
            ? "Nicht angemeldet. Ohne eigene Anmeldung wird das Token einer laufenden "
              + "Claude-Code-Installation mitgelesen."
            : $"Angemeldet. Rechte: {tokens.Scope ?? "unbekannt"}. "
              + $"Token gültig bis {tokens.ExpiresAt?.ToLocalTime():g} und wird selbsttätig erneuert.";

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

    /// <summary>Wird ausgeloest, wenn Einstellungen oder Token geaendert wurden.</summary>
    public event EventHandler<AppSettings>? SettingsChanged;

    private void UpdateTokenStatus()
    {
        if (!_secretStore.IsSupported)
        {
            TokenStatus.Text = "Auf diesem System steht kein sicherer Speicher zur Verfuegung.";
            SaveTokenButton.IsEnabled = false;
            DeleteTokenButton.IsEnabled = false;
            return;
        }

        var hasToken = !string.IsNullOrEmpty(_secretStore.Read(SecretStoreTokenProvider.DefaultKey));
        TokenStatus.Text = hasToken
            ? "Ein Token ist hinterlegt."
            : "Kein eigenes Token hinterlegt - es wird versucht, das Token der "
              + "Claude-Code-Installation mitzulesen.";
        DeleteTokenButton.IsEnabled = hasToken;
    }

    private async void SaveToken()
    {
        var value = TokenBox.Text?.Trim();
        if (string.IsNullOrEmpty(value))
        {
            TokenStatus.Text = "Bitte zuerst ein Token einfügen.";
            return;
        }

        // Erst pruefen, dann speichern: Ein untaugliches Token soll gar nicht
        // erst in den Secret-Store gelangen.
        if (_validateToken is not null)
        {
            SaveTokenButton.IsEnabled = false;
            TokenStatus.Text = "Token wird geprüft ...";
            try
            {
                var result = await _validateToken(value).ConfigureAwait(true);
                if (!result.IsUsable)
                {
                    TokenStatus.Text = "Nicht gespeichert. " + result.Message;
                    return;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                TokenStatus.Text = "Die Pruefung ist fehlgeschlagen: " + ex.Message;
                return;
            }
            finally
            {
                SaveTokenButton.IsEnabled = true;
            }
        }

        try
        {
            _secretStore.Write(SecretStoreTokenProvider.DefaultKey, value);
            TokenBox.Text = string.Empty;
            UpdateTokenStatus();
            SettingsChanged?.Invoke(this, _settings);
        }
        catch (InvalidOperationException ex)
        {
            TokenStatus.Text = ex.Message;
        }
    }

    private void DeleteToken()
    {
        try
        {
            _secretStore.Delete(SecretStoreTokenProvider.DefaultKey);
            UpdateTokenStatus();
            SettingsChanged?.Invoke(this, _settings);
        }
        catch (InvalidOperationException ex)
        {
            TokenStatus.Text = ex.Message;
        }
    }

    private void SaveAndClose()
    {
        _settings = new AppSettings
        {
            PollIntervalSeconds = (int)(IntervalBox.Value ?? 300),
            LaunchAtLogin = LaunchAtLoginBox.IsChecked ?? false,
            CheckForUpdates = CheckUpdatesBox.IsChecked ?? true,
            WarningThreshold = _settings.WarningThreshold,
            CriticalThreshold = _settings.CriticalThreshold
        };

        _settingsStore.Save(_settings);
        AutostartManager.Apply(_settings.LaunchAtLogin);
        SettingsChanged?.Invoke(this, _settings);
        Close();
    }
}
