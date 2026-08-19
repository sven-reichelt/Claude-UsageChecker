// Claude UsageChecker - shows the usage limits of a Claude subscription in the
// Windows notification area.
// Copyright (C) 2026 Sven Reichelt
//
// This program is free software: you can redistribute it and/or modify it under
// the terms of the GNU General Public License as published by the Free Software
// Foundation, either version 3 of the License, or (at your option) any later
// version.
//
// This program is distributed in the hope that it will be useful, but WITHOUT
// ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
// FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License along with
// this program. If not, see <https://www.gnu.org/licenses/>.

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
