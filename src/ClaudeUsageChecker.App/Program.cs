using System;
using Avalonia;

namespace ClaudeUsageChecker.App;

internal static class Program
{
    // No initialisation before Avalonia starts: the synchronization context and
    // logging are only set up here.
    [STAThread]
    public static int Main(string[] args)
    {
        CrashReporter.InstallGlobalHandlers();

        // After an update the replaced version runs a moment longer and holds
        // the single-instance lock. Wait for it, then continue.
        StartupArguments.WaitForPredecessor(args);
        Services.UpdateInstaller.RemovePreviousVersion();

        // A second instance would add a second tray icon and poll the API twice.
        // It therefore ends itself without a word.
        using var instance = SingleInstance.TryAcquire();
        if (instance is null)
        {
            return 0;
        }

        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            return 0;
        }
        catch (Exception ex)
        {
            CrashReporter.Write(ex, "Program.Main");
            return 1;
        }
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
