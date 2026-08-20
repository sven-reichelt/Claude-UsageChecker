using System;
using System.IO;
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
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // A failed autostart entry must not disturb the application.
        }
    }

    /// <summary>
    /// The property list. <c>open -a</c> rather than the executable: it hands
    /// the start to macOS, which then treats the program as the bundled
    /// application it is.
    /// </summary>
    internal static string BuildPlist(string program) =>
        $"""
         <?xml version="1.0" encoding="UTF-8"?>
         <!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
         <plist version="1.0">
         <dict>
             <key>Label</key>
             <string>{Label}</string>
             <key>ProgramArguments</key>
             <array>
                 <string>/usr/bin/open</string>
                 <string>-a</string>
                 <string>{Escape(program)}</string>
             </array>
             <key>RunAtLoad</key>
             <true/>
         </dict>
         </plist>

         """;

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
