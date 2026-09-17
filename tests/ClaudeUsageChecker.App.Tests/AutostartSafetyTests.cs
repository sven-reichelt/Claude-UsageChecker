using System.Runtime.CompilerServices;
using ClaudeUsageChecker.App.Views;
using Microsoft.Win32;

namespace ClaudeUsageChecker.App.Tests;

/// <summary>
/// Switches off every autostart write before the first test runs.
/// </summary>
/// <remarks>
/// The settings window writes the real autostart entry when "save" is pressed,
/// unless a test hands it a switch of its own. One test did not, and every run
/// of the suite deleted the autostart entry of the machine it ran on - which
/// looked like updates dropping the application from autostart, because the
/// suite runs right before every release. A module initializer runs before any
/// test in this assembly, so no test can forget it.
/// </remarks>
internal static class AutostartSafety
{
    [ModuleInitializer]
    internal static void KeepTheTestsAwayFromTheRealAutostart() => AutostartManager.WritesDisabled = true;
}

/// <summary>
/// That the suite cannot touch the autostart entry, and how an entry that went
/// missing is recognised.
/// </summary>
public class AutostartSafetyTests
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

    [Fact]
    public void TheTestsCannotWriteTheRealAutostart()
    {
        // First, before anything is called: if the guard were missing, the call
        // below would delete the entry of whoever runs this.
        Assert.True(AutostartManager.WritesDisabled,
            "The test assembly must switch autostart writes off before any test runs.");

        var before = ReadRunValue();

        AutostartManager.Apply(enabled: false);
        var restored = AutostartManager.Restore(@"C:\nowhere\ClaudeUsageChecker.exe");

        Assert.Equal(before, ReadRunValue());
        Assert.False(restored);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("\"C:\\Users\\x\\AppData\\Local\\Programs\\ClaudeUsageChecker\\ClaudeUsageChecker.exe\"", false)]
    [InlineData("\"c:\\users\\x\\appdata\\local\\programs\\claudeusagechecker\\claudeusagechecker.exe\"", false)]
    [InlineData("\"C:\\Users\\x\\Downloads\\ClaudeUsageChecker.exe\"", true)]
    [InlineData("C:\\Users\\x\\AppData\\Local\\Programs\\ClaudeUsageChecker\\ClaudeUsageChecker.exe", true)]
    public void NeedsRestoring_OnlyWhereTheEntryIsMissingOrPointsElsewhere(string? current, bool expected)
    {
        const string installed = @"C:\Users\x\AppData\Local\Programs\ClaudeUsageChecker\ClaudeUsageChecker.exe";

        Assert.Equal(expected, AutostartManager.NeedsRestoring(current, installed));
    }

    /// <summary>
    /// The value is quoted: a profile path with a space in it would otherwise be
    /// started as a program called "C:\Users\Sven".
    /// </summary>
    [Fact]
    public void RunValueFor_QuotesThePath()
    {
        Assert.Equal("\"C:\\A B\\x.exe\"", AutostartManager.RunValueFor(@"C:\A B\x.exe"));
    }

    private static string? ReadRunValue()
    {
        if (!OperatingSystem.IsWindows())
        {
            return null;
        }

        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: false);
        return key?.GetValue("ClaudeUsageChecker") as string;
    }
}
