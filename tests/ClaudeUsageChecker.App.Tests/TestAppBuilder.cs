using Avalonia;
using Avalonia.Headless;
using ClaudeUsageChecker.App;
using Xunit;

[assembly: AvaloniaTestApplication(typeof(ClaudeUsageChecker.App.Tests.TestAppBuilder))]

// The selected language is process-wide state: Localizer.Use switches it for
// everyone, and it sets the culture along with it. Test classes running side by
// side therefore pull the rug from under each other - the labelling tests switch
// to German while the token source labels are being read in English. That is a
// race, and a race that shows up as a failure only sometimes is worse than a
// slower run.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

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
