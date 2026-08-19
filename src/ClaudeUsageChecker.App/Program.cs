using System;
using Avalonia;

namespace ClaudeUsageChecker.App;

internal static class Program
{
    // Initialisierung vor dem Start von Avalonia ist nicht erlaubt:
    // SynchronizationContext und Logging werden erst hier aufgesetzt.
    [STAThread]
    public static int Main(string[] args)
    {
        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            return 0;
        }
        catch (Exception ex)
        {
            CrashReporter.Write(ex);
            return 1;
        }
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
