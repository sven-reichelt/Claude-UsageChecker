using System;
using System.IO;
using System.Linq;
using System.Text;

namespace ClaudeUsageChecker.App.Services;

/// <summary>
/// Autostart on macOS, through a launch agent of the current user.
/// </summary>
/// <remarks>
/// <para>
/// A property list under <c>~/Library/LaunchAgents</c> is what macOS intends for
/// this: it belongs to the user, needs no administrator rights, and launchd
/// starts it at login. The alternative - a login item through the System
/// Settings database - is not reachable without private interfaces.
/// </para>
/// <para>
/// What is registered is the application bundle, not the executable inside it.
/// Starting the executable directly gives a process without a bundle, and macOS
/// treats those differently: no bundle identifier, and the menu bar item ends up
/// in a worse place than it should.
/// </para>
/// <para>
/// Nothing here calls into macOS: it writes a file and formats some text.
/// The class is therefore not marked as macOS-only, which would make the
/// property list unreadable to a test running anywhere else - and that text
/// is exactly the part worth testing.
/// </para>
/// </remarks>
internal static class MacOsLaunchAgent
{
    /// <summary>Reverse domain name, as launchd expects for a label.</summary>
    public const string Label = "de.sven-reichelt.claudeusagechecker";

    /// <summary>Where the property list lives.</summary>
    public static string PlistPath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        "Library", "LaunchAgents", $"{Label}.plist");

    /// <summary>
    /// Writes or removes the agent.
    /// </summary>
    /// <param name="path">
    /// The path to register. Without one the running program is taken - but when
    /// installing it is the target that has to be registered, not wherever the
    /// application happens to run from at that moment.
    /// </param>
    public static void Apply(bool enabled, string? path = null)
    {
        try
        {
            if (!enabled)
            {
                if (File.Exists(PlistPath))
                {
                    File.Delete(PlistPath);
                }

                return;
            }

            var program = path ?? Environment.ProcessPath;
            if (string.IsNullOrEmpty(program))
            {
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(PlistPath)!);
            File.WriteAllText(PlistPath, BuildPlist(program), new UTF8Encoding(false));

            // launchd reads the file at login; a change now would otherwise
            // wait for the next one. Failure is not checked - the file is in
            // place, which is what the next login needs.
            TryBootstrap();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // A failed autostart entry must not disturb the application.
        }
    }

    /// <summary>
    /// What launchd is told to run.
    /// </summary>
    /// <remarks>
    /// The caller hands over whatever it has - the settings window only knows
    /// <c>Environment.ProcessPath</c>, which is the executable *inside* the
    /// bundle. Registering that with <c>open -a</c> fails outright: open wants
    /// an application, not a file in one. So the bundle is derived from the
    /// path here, where the knowledge of what a bundle looks like belongs.
    /// Without a bundle around the executable - a bare build - the executable
    /// itself is started directly, which is the only thing that works then.
    /// </remarks>
    internal static string[] ProgramArguments(string program)
    {
        return BundleOf(program) is { } bundle
            ? ["/usr/bin/open", "-a", bundle]
            : [program];
    }

    /// <summary>
    /// The .app the file lives in, or null where there is none.
    /// </summary>
    internal static string? BundleOf(string program)
    {
        // By text, not through Path: that class bends separators to the
        // platform it runs on, and these are macOS paths whatever machine this
        // code happens to execute on - the tests included.
        for (var current = program; !string.IsNullOrEmpty(current);)
        {
            if (current.EndsWith(".app", StringComparison.OrdinalIgnoreCase))
            {
                return current;
            }

            var cut = current.LastIndexOf('/');
            if (cut <= 0)
            {
                return null;
            }

            current = current[..cut];
        }

        return null;
    }

    /// <summary>Asks launchd to pick the agent up now rather than at the next login.</summary>
    private static void TryBootstrap()
    {
        try
        {
            using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("/bin/launchctl")
            {
                ArgumentList =
                {
                    "bootstrap",
                    $"gui/{Native.getuid()}",
                    PlistPath
                },
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            });
            process?.WaitForExit(5000);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // Already loaded, or an older launchctl - the file alone suffices
            // at the next login either way.
        }
    }

    private static class Native
    {
        [System.Runtime.InteropServices.DllImport("libc")]
        internal static extern uint getuid();
    }

    /// <summary>The property list, built from <see cref="ProgramArguments"/>.</summary>
    internal static string BuildPlist(string program)
    {
        var arguments = string.Join("\n", ProgramArguments(program)
            .Select(a => $"        <string>{Escape(a)}</string>"));

        return $"""
         <?xml version="1.0" encoding="UTF-8"?>
         <!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
         <plist version="1.0">
         <dict>
             <key>Label</key>
             <string>{Label}</string>
             <key>ProgramArguments</key>
             <array>
         {arguments}
             </array>
             <key>RunAtLoad</key>
             <true/>
         </dict>
         </plist>

         """;
    }

    /// <summary>
    /// A path is text inside XML. Applications folders are allowed ampersands
    /// and angle brackets, however unlikely, and a mangled plist is one launchd
    /// silently ignores.
    /// </summary>
    private static string Escape(string value) => value
        .Replace("&", "&amp;", StringComparison.Ordinal)
        .Replace("<", "&lt;", StringComparison.Ordinal)
        .Replace(">", "&gt;", StringComparison.Ordinal);
}
