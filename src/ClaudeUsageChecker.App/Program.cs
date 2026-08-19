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
        CrashReporter.InstallGlobalHandlers();

        // Nach einer Aktualisierung laeuft die ersetzte Fassung noch kurz und
        // haelt den Riegel auf eine Instanz. Erst abwarten, dann weiter.
        StartupArguments.WaitForPredecessor(args);
        Services.UpdateInstaller.RaeumeAltfassungWeg();

        // Eine zweite Instanz wuerde ein zweites Symbol im Infobereich anlegen
        // und die API doppelt abfragen. Sie beendet sich deshalb stillschweigend.
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
