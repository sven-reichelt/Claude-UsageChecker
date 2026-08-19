using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using ClaudeUsageChecker.App.Settings;
using ClaudeUsageChecker.App.Views;
using ClaudeUsageChecker.Core.Authentication.OAuth;

namespace ClaudeUsageChecker.App.Tests;

/// <summary>Checks the windows around the application's own sign-in.</summary>
public class SignInWindowTests
{
    [AvaloniaFact]
    public void TheSignInWindowCanBeCreated()
    {
        var window = new SignInWindow(CreateOAuthClient(), CreateTokenStore(out _));

        Assert.NotNull(window.FindControl<Button>("OpenBrowserButton"));
        Assert.NotNull(window.FindControl<TextBox>("CodeBox"));
        Assert.NotNull(window.FindControl<Button>("CompleteButton"));
    }

    [AvaloniaFact]
    public void WithoutAStartedSignInNothingCanBeCompleted()
    {
        var window = new SignInWindow(CreateOAuthClient(), CreateTokenStore(out _));

        Assert.False(window.FindControl<Button>("CompleteButton")!.IsEnabled);
    }

    [AvaloniaFact]
    public void WithoutASignInTheWindowSaysSoPlainly()
    {
        var window = new SignInWindow(CreateOAuthClient(), CreateTokenStore(out _));

        Assert.Contains("Not signed in yet",
            window.FindControl<TextBlock>("SignedInText")!.Text!, StringComparison.Ordinal);
    }

    [AvaloniaFact]
    public void AnExistingSignInIsShownWithItsScope()
    {
        var store = CreateTokenStore(out _);
        store.Write(new OAuthTokens
        {
            AccessToken = "a1",
            RefreshToken = "r1",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(8),
            Scope = "user:profile"
        });

        var window = new SignInWindow(CreateOAuthClient(), store);

        var text = window.FindControl<TextBlock>("SignedInText")!.Text!;
        Assert.Contains("Signed in", text, StringComparison.Ordinal);
        Assert.Contains("user:profile", text, StringComparison.Ordinal);
    }

    [AvaloniaFact]
    public void TheSettingsShowTheSignInState()
    {
        using var settingsFile = new TemporaryFile();
        var store = CreateTokenStore(out _);

        var window = new SettingsWindow(
            new SettingsStore(settingsFile.Path), new AppSettings(),
            oauthTokenStore: store);

        Assert.Contains("Not signed in",
            window.FindControl<TextBlock>("SignInStatus")!.Text!, StringComparison.Ordinal);
        Assert.False(window.FindControl<Button>("SignOutButton")!.IsEnabled);
    }

    [AvaloniaFact]
    public void TheSettingsAllowSigningOutOnlyWhenSignedIn()
    {
        using var settingsFile = new TemporaryFile();
        var store = CreateTokenStore(out _);
        store.Write(new OAuthTokens { AccessToken = "a1", Scope = "user:profile" });

        var window = new SettingsWindow(
            new SettingsStore(settingsFile.Path), new AppSettings(),
            oauthTokenStore: store);

        Assert.True(window.FindControl<Button>("SignOutButton")!.IsEnabled);
        Assert.Contains("user:profile",
            window.FindControl<TextBlock>("SignInStatus")!.Text!, StringComparison.Ordinal);
    }

    private static AnthropicOAuthClient CreateOAuthClient() =>
        new(new HttpClient(), new OAuthOptions());

    private static OAuthTokenStore CreateTokenStore(out FakeSecretStore secretStore)
    {
        secretStore = new FakeSecretStore();
        return new OAuthTokenStore(secretStore);
    }

    private sealed class TemporaryFile : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(), $"cuc-test-{Guid.NewGuid():N}.json");

        public void Dispose()
        {
            if (File.Exists(Path))
            {
                File.Delete(Path);
            }
        }
    }
}
