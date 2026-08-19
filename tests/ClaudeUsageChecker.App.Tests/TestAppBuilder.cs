using Avalonia;
using Avalonia.Headless;
using ClaudeUsageChecker.App;

[assembly: AvaloniaTestApplication(typeof(ClaudeUsageChecker.App.Tests.TestAppBuilder))]

namespace ClaudeUsageChecker.App.Tests;

/// <summary>
/// Starts the real application class on the headless Avalonia platform. The
/// composition of services is skipped, because it is tied to a desktop lifetime -
/// which is exactly what is wanted here.
/// </summary>
public static class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>().UseHeadless(new AvaloniaHeadlessPlatformOptions());
}
