using Avalonia;
using Avalonia.Headless;
using ClaudeUsageChecker.App;

[assembly: AvaloniaTestApplication(typeof(ClaudeUsageChecker.App.Tests.TestAppBuilder))]

namespace ClaudeUsageChecker.App.Tests;

/// <summary>
/// Startet die echte Anwendungsklasse auf der kopflosen Avalonia-Plattform.
/// Die Zusammenstellung der Dienste bleibt dabei aus, weil sie an eine
/// Desktop-Lebensdauer gebunden ist - genau das ist hier erwuenscht.
/// </summary>
public static class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>().UseHeadless(new AvaloniaHeadlessPlatformOptions());
}
