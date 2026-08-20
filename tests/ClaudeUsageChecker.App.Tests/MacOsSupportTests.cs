using Avalonia.Headless.XUnit;
using System.Reflection;
using System.Runtime.InteropServices;
using ClaudeUsageChecker.App.Services;
using ClaudeUsageChecker.Core.Localization;
using ClaudeUsageChecker.Core.Platform;

namespace ClaudeUsageChecker.App.Tests;

/// <summary>
/// Checks what can be checked about the macOS support from anywhere.
/// </summary>
/// <remarks>
/// <para>
/// Most of it cannot be: whether a status item appears in the menu bar, whether
/// the keychain accepts an entry, whether launchd honours the agent - none of
/// that answers on a build machine running Windows. What is left is the part
/// that is pure arithmetic and text, and that part is worth pinning down,
/// because it is also the part that is easy to get wrong by a character and
/// hard to notice: a property list launchd silently ignores looks exactly like
/// one it honours.
/// </para>
/// <para>
/// The rest has to be tried on a Mac, and this file makes no pretence
/// otherwise.
/// </para>
/// </remarks>
public class MacOsSupportTests
{
    /// <summary>
    /// The property list is well-formed XML and names what launchd needs.
    /// </summary>
    [Fact]
    public void TheLaunchAgentNamesTheProgramAndAsksToRunAtLogin()
    {
        var values = Strings("/Applications/ClaudeUsageChecker.app");

        Assert.Contains(MacOsLaunchAgent.Label, values);
        Assert.Contains("/usr/bin/open", values);
        Assert.Contains("/Applications/ClaudeUsageChecker.app", values);
        Assert.Contains("RunAtLoad", MacOsLaunchAgent.BuildPlist("/x.app"), StringComparison.Ordinal);
    }

    /// <summary>
    /// A path with an ampersand in it must not break the file.
    /// </summary>
    /// <remarks>
    /// Unlikely in an applications folder, and cheap to get right. The failure
    /// mode is the bad one: launchd does not complain about a malformed list, it
    /// just never starts the program.
    /// </remarks>
    [Fact]
    public void TheLaunchAgentSurvivesAnAwkwardPath()
    {
        // Read back as a parser reads it, not as text: the point is that the
        // path arrives intact on the other side, and escaped text looks nothing
        // like the original until it has been parsed.
        Assert.Contains("/Users/a&b/Applications/<x>.app", Strings("/Users/a&b/Applications/<x>.app"));
    }

    /// <summary>The values of every string element of the property list.</summary>
    private static List<string> Strings(string program) =>
        [.. System.Xml.Linq.XDocument.Parse(MacOsLaunchAgent.BuildPlist(program))
            .Descendants("string")
            .Select(e => e.Value)];

    /// <summary>
    /// The executable inside a bundle registers the bundle, not itself.
    /// </summary>
    /// <remarks>
    /// This is the path the settings window actually takes: it knows only
    /// Environment.ProcessPath, which points at Contents/MacOS/… inside the
    /// bundle. "open -a" with that file fails outright, and launchd does not
    /// complain about an agent that fails - autostart would simply never
    /// happen. Found in review, not on a machine.
    /// </remarks>
    [Fact]
    public void TheLaunchAgentRegistersTheBundleWhenGivenTheExecutableInsideIt()
    {
        var values = Strings(
            "/Applications/ClaudeUsageChecker.app/Contents/MacOS/ClaudeUsageChecker");

        Assert.Contains("/usr/bin/open", values);
        Assert.Contains("/Applications/ClaudeUsageChecker.app", values);
        Assert.DoesNotContain(
            "/Applications/ClaudeUsageChecker.app/Contents/MacOS/ClaudeUsageChecker", values);
    }

    /// <summary>A bare executable without a bundle is started directly.</summary>
    [Fact]
    public void TheLaunchAgentStartsABareExecutableDirectly()
    {
        var values = Strings("/Users/tester/bin/ClaudeUsageChecker");

        Assert.DoesNotContain("/usr/bin/open", values);
        Assert.Contains("/Users/tester/bin/ClaudeUsageChecker", values);
    }

    /// <summary>
    /// The application knows its own name.
    /// </summary>
    /// <remarks>
    /// macOS shows it in the menu bar as soon as a window takes focus, and
    /// builds "About …" and "Quit …" from it. Avalonia calls itself "Avalonia
    /// Application" until told otherwise - which is what stood there.
    /// </remarks>
    [AvaloniaFact]
    public void TheApplicationIsNamedAfterTheProduct()
    {
        var name = Avalonia.Application.Current?.Name;

        Assert.Equal(T.AppName, name);
        Assert.DoesNotContain("Avalonia", name ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// The application menu stands before anything can read it.
    /// </summary>
    /// <remarks>
    /// Avalonia's exporter reads this property once and, finding nothing,
    /// builds its own menu - the one whose first entry says "About Avalonia" -
    /// and writes that into the very same property. Setting it afterwards
    /// changes a value nobody reads again, which is exactly what the first
    /// attempt did. The test therefore asks the question that matters: is it
    /// there once Initialize has run?
    ///
    /// Only on macOS is it built at all; elsewhere nothing shows it and the
    /// property stays empty.
    ///
    /// The labels come out of the language file, which means the language has
    /// to be settled before the menu is built. It was not, at first: the
    /// application came up in German with an English menu beside the apple.
    /// </remarks>
    [AvaloniaFact]
    public void TheApplicationMenuIsInPlaceAfterInitialisation()
    {
        var application = Avalonia.Application.Current!;
        var menu = Avalonia.Controls.NativeMenu.GetMenu(application);

        if (!OperatingSystem.IsMacOS())
        {
            Assert.Null(menu);
            return;
        }

        Assert.NotNull(menu);

        var headers = menu.Items
            .OfType<Avalonia.Controls.NativeMenuItem>()
            .Select(i => i.Header)
            .ToList();

        Assert.Contains(T.AboutTitle, headers);
        Assert.Contains(T.TraySettings, headers);
        Assert.DoesNotContain(headers, h => h?.Contains("Avalonia", StringComparison.OrdinalIgnoreCase) == true);

        // Nothing for leaving: macOS adds Quit to this menu by itself, and one
        // of our own beside it was the same thing twice on the same shortcut.
        Assert.DoesNotContain(T.TrayExit, headers);
    }

    /// <summary>
    /// The application menu follows a change of language.
    /// </summary>
    /// <remarks>
    /// A menu already handed to macOS does not relabel itself; the entries stay
    /// in the language they were built in, which is how the application ended
    /// up Italian with a German menu beside the apple. Handing over a fresh one
    /// is what reaches the system.
    ///
    /// Only answerable where the menu exists at all, so this steps aside on
    /// Windows - the macOS build in CI runs the same suite and does answer it.
    /// </remarks>
    [AvaloniaFact]
    public void TheApplicationMenuFollowsAChangeOfLanguage()
    {
        if (!OperatingSystem.IsMacOS())
        {
            return;
        }

        var application = Avalonia.Application.Current!;
        var before = Localizer.Current.Language;

        try
        {
            Localizer.Use(Language.Find("de")!);
            ((App)application).GetType()
                .GetMethod("BuildMacOsApplicationMenu", BindingFlags.NonPublic | BindingFlags.Instance)!
                .Invoke(application, null);

            var german = Headers(application);
            Assert.Contains(T.AboutTitle, german);

            Localizer.Use(Language.Find("it")!);
            ((App)application).GetType()
                .GetMethod("BuildMacOsApplicationMenu", BindingFlags.NonPublic | BindingFlags.Instance)!
                .Invoke(application, null);

            var italian = Headers(application);

            Assert.Contains(T.AboutTitle, italian);
            Assert.NotEqual(german, italian);
        }
        finally
        {
            Localizer.Use(before);
        }
    }

    private static List<string?> Headers(Avalonia.Application application) =>
        [.. Avalonia.Controls.NativeMenu.GetMenu(application)!.Items
            .OfType<Avalonia.Controls.NativeMenuItem>()
            .Select(i => i.Header)];

    /// <summary>The label is a reverse domain name, as launchd expects.</summary>
    [Fact]
    public void TheLabelIsAReverseDomainName()
    {
        Assert.Matches(@"^[a-z0-9-]+(\.[a-z0-9-]+)+$", MacOsLaunchAgent.Label);
    }

    /// <summary>
    /// The lock of the single instance carries no backslash where a backslash is
    /// a character in a file name rather than a scope.
    /// </summary>
    /// <remarks>
    /// Windows reads "Local\" as "this login session". On macOS and Linux the
    /// name becomes part of a file name, and the prefix would be neither
    /// understood nor harmless.
    ///
    /// The name is checked rather than the lock itself: taking it would say more
    /// about what happens to be running on the machine than about the code - the
    /// first attempt at this test went red because the application was open on
    /// the developer's desktop at the time.
    /// </remarks>
    [Fact]
    public void TheSingleInstanceLockIsNamedForTheRunningPlatform()
    {
        var name = SingleInstance.MutexName;

        if (OperatingSystem.IsWindows())
        {
            Assert.StartsWith(@"Local\", name, StringComparison.Ordinal);
        }
        else
        {
            Assert.DoesNotContain(@"\", name, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// Every platform gets a store, and the one it gets says whether it works.
    /// </summary>
    [Fact]
    public void TheSecretStoreFitsTheRunningPlatform()
    {
        var store = SecretStoreFactory.CreateForCurrentPlatform();

        if (OperatingSystem.IsWindows() || OperatingSystem.IsMacOS())
        {
            Assert.True(store.IsSupported);
        }
        else
        {
            Assert.False(store.IsSupported);
        }
    }

    /// <summary>
    /// The keychain is reached through the Security framework rather than the
    /// command line tool.
    /// </summary>
    /// <remarks>
    /// Not a matter of taste. <c>security add-generic-password</c> takes the
    /// password as an argument, and the arguments of a running process are
    /// readable by every account on the machine. This test cannot see the
    /// keychain, but it can see that the class is built on the framework - and
    /// it is the reminder for whoever later finds the tool simpler.
    /// </remarks>
    [Fact]
    public void TheKeychainIsReachedThroughTheFrameworkAndNotTheCommandLine()
    {
        var source = typeof(MacOsKeychainStore).Assembly.Location;

        Assert.NotNull(source);

        var methods = typeof(MacOsKeychainStore)
            .GetNestedTypes(System.Reflection.BindingFlags.NonPublic)
            .SelectMany(t => t.GetMethods(
                System.Reflection.BindingFlags.NonPublic
                | System.Reflection.BindingFlags.Static
                | System.Reflection.BindingFlags.DeclaredOnly))
            .Where(m => m.GetCustomAttributes(typeof(DllImportAttribute), inherit: false).Length > 0)
            .Select(m => m.Name)
            .ToList();

        Assert.Contains("SecItemAdd", methods);
        Assert.Contains("SecItemCopyMatching", methods);
        Assert.Contains("SecItemDelete", methods);
    }
}
