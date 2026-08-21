using ClaudeUsageChecker.App.Services;

namespace ClaudeUsageChecker.App.Tests;

/// <summary>
/// The part of the macOS self-replacement that answers on any machine.
/// </summary>
/// <remarks>
/// <para>
/// Whether ditto unpacks, whether codesign is satisfied, whether open starts
/// the new bundle - none of that can be asked here, and the workflow asks it on
/// a Mac instead. What is left is the reasoning around those calls: which
/// directory counts as the bundle, whether the running application may replace
/// itself at all, and whether the version set aside is found again afterwards.
/// </para>
/// <para>
/// That is the part with the expensive failure mode. Getting the bundle wrong
/// means moving the wrong directory aside, and by then the old version is
/// already gone.
/// </para>
/// </remarks>
public class MacOsBundleTests
{
    /// <summary>The replaced version is started as a new instance.</summary>
    /// <remarks>
    /// Found on a Mac, not here: the update ran through, the new version was in
    /// place, and nothing was running afterwards - it had to be started by
    /// hand. Without <c>-n</c>, open does not start a second instance of an
    /// application it considers already running; it activates the running one,
    /// which at that moment is the version being replaced. It then reports
    /// success and quits, and no new process ever existed.
    /// <para>
    /// The counter-check that matters: take the flag away again and this test
    /// has to fail. It was written that way round.
    /// </para>
    /// </remarks>
    [Fact]
    public void TheReplacedVersionIsStartedAsANewInstance()
    {
        var arguments = MacOsBundle.StartArguments("/Applications/ClaudeUsageChecker.app", 4711);

        Assert.Equal("-n", arguments[0]);
        Assert.Equal(["-a", "/Applications/ClaudeUsageChecker.app"], arguments[1..3]);
    }

    /// <summary>Gatekeeper is asked the question it will be asked later.</summary>
    /// <remarks>
    /// This is the safeguard that decides whether a downloaded bundle is run at
    /// all, and it asked <c>--type install</c> - the rule set for installer
    /// packages, not applications. It therefore approved things macOS itself
    /// would refuse. The same wrong question stood in the release workflow and
    /// was found the same day, from the other end: it reported "accepted" for a
    /// bundle a Mac was turning away.
    /// </remarks>
    [Fact]
    public void TheDownloadedBundleIsAssessedAsAnApplication()
    {
        var arguments = MacOsBundle.GatekeeperArguments("/tmp/ClaudeUsageChecker.app");

        Assert.Equal(["--assess", "--type", "execute", "/tmp/ClaudeUsageChecker.app"], arguments);
        Assert.DoesNotContain("install", arguments);
    }

    /// <summary>The new instance is told which process to wait for.</summary>
    /// <remarks>
    /// The other half of the handshake, and useless without the first: the
    /// second instance waits for this one to end, clears away what it left, and
    /// only then takes the single-instance lock. A new instance that arrived
    /// without the number would find the lock held and end itself without a
    /// word - the same empty Mac by a different route.
    /// </remarks>
    [Fact]
    public void TheNewInstanceIsToldWhichProcessToWaitFor()
    {
        var arguments = MacOsBundle.StartArguments("/Applications/ClaudeUsageChecker.app", 4711);

        var args = Array.IndexOf(arguments, "--args");
        Assert.NotEqual(-1, args);
        Assert.Equal(UpdateInstaller.WaitArgument, arguments[args + 1]);
        Assert.Equal("4711", arguments[args + 2]);
    }

    /// <summary>The executable inside a bundle names the bundle.</summary>
    /// <remarks>
    /// This is what the program knows about itself: ProcessPath points into
    /// Contents/MacOS, and everything that follows - moving aside, replacing,
    /// starting - is about the .app four levels above it.
    /// </remarks>
    [Fact]
    public void TheBundleIsFoundFromTheExecutableInsideIt() =>
        Assert.Equal(
            "/Applications/ClaudeUsageChecker.app",
            MacOsBundle.Of("/Applications/ClaudeUsageChecker.app/Contents/MacOS/ClaudeUsageChecker"));

    /// <summary>A path that is not in a bundle has none.</summary>
    /// <remarks>
    /// A development build runs straight out of a build folder. It must not
    /// find a bundle there, because the answer decides whether the program
    /// offers to replace itself.
    /// </remarks>
    [Theory]
    [InlineData("/Users/tester/bin/ClaudeUsageChecker")]
    [InlineData("/ClaudeUsageChecker")]
    [InlineData("")]
    public void APathOutsideABundleHasNoBundle(string path) =>
        Assert.Null(MacOsBundle.Of(path));

    /// <summary>
    /// The paths are read as macOS writes them, whatever machine reads them.
    /// </summary>
    /// <remarks>
    /// Path.GetDirectoryName bends the separators to the platform it runs on,
    /// so on Windows it would walk this path not at all and hand back the whole
    /// string. Same reasoning as in the launch agent, which was bitten by it.
    /// </remarks>
    [Fact]
    public void TheSeparatorIsTheOneMacOsUsesAndNotTheOneThisMachineUses() =>
        Assert.Equal(
            "/Applications/Some Folder/ClaudeUsageChecker.app",
            MacOsBundle.Of("/Applications/Some Folder/ClaudeUsageChecker.app/Contents/MacOS/x"));

    /// <summary>Without a bundle nothing can be replaced.</summary>
    [Fact]
    public void AProgramOutsideABundleCannotReplaceItself()
    {
        Assert.False(MacOsBundle.CanReplace("/Users/tester/bin/ClaudeUsageChecker"));
        Assert.False(MacOsBundle.CanReplace(null));
    }

    /// <summary>
    /// A bundle whose folder does not exist cannot be replaced either.
    /// </summary>
    /// <remarks>
    /// The check writes a file beside the bundle and removes it again. That is
    /// deliberately the real question - an application in /Applications belongs
    /// to whoever installed it, and another account may be able to read it and
    /// not to replace it. Finding that out halfway through the swap would leave
    /// nothing runnable behind.
    /// </remarks>
    [Fact]
    public void ABundleInAFolderThatCannotBeWrittenToCannotBeReplaced() =>
        Assert.False(MacOsBundle.CanReplace(
            Path.Combine(Path.GetTempPath(), $"cuc-{Guid.NewGuid():N}", "X.app", "Contents", "MacOS", "X")));

    /// <summary>A bundle in a writable folder can be replaced.</summary>
    [Fact]
    public void ABundleInAFolderThatCanBeWrittenToCanBeReplaced()
    {
        var folder = Directory.CreateTempSubdirectory("cuc-").FullName;

        try
        {
            // Built as text: the class reads macOS paths, and Path.Combine
            // would hand it backslashes on this machine.
            Assert.True(MacOsBundle.CanReplace(
                $"{AsMacOsPath(folder)}/ClaudeUsageChecker.app/Contents/MacOS/ClaudeUsageChecker"));

            // And nothing of the probe is left lying around.
            Assert.Empty(Directory.GetFileSystemEntries(folder));
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    /// <summary>
    /// The version set aside by the update is cleared away on the next start.
    /// </summary>
    /// <remarks>
    /// It cannot be deleted at the moment of the swap, because it is still
    /// running then. So it stays until the new version starts, and if that
    /// never happens it is what the old one is restored from.
    /// </remarks>
    [Fact]
    public void TheVersionSetAsideIsClearedAwayOnTheNextStart()
    {
        var folder = Directory.CreateTempSubdirectory("cuc-").FullName;

        try
        {
            var bundle = Path.Combine(folder, "ClaudeUsageChecker.app");
            var setAside = bundle + UpdateInstaller.BackupSuffix;
            Directory.CreateDirectory(Path.Combine(setAside, "Contents", "MacOS"));
            File.WriteAllText(Path.Combine(setAside, "Contents", "MacOS", "x"), "old");

            var executable = $"{AsMacOsPath(bundle)}/Contents/MacOS/ClaudeUsageChecker";
            MacOsBundle.RemovePreviousVersion(executable);

            Assert.False(Directory.Exists(setAside));
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    /// <summary>
    /// A path of this machine, written the way macOS writes one.
    /// </summary>
    /// <remarks>
    /// The tests need a real directory, and a real directory here carries
    /// backslashes. The class under test reads macOS paths, so they are
    /// translated rather than handed over as they are.
    /// </remarks>
    private static string AsMacOsPath(string path) =>
        path.Replace(Path.DirectorySeparatorChar, '/');

    /// <summary>Outside a bundle it removes nothing, and does not complain.</summary>
    /// <remarks>
    /// Every start goes through here, including the ones that were never
    /// updated. It has to be silent about finding nothing.
    /// </remarks>
    [Fact]
    public void NothingSetAsideIsNoReasonToComplain()
    {
        MacOsBundle.RemovePreviousVersion(null);
        MacOsBundle.RemovePreviousVersion("/Users/tester/bin/ClaudeUsageChecker");
    }

    /// <summary>
    /// The permanent setup into a Windows folder is not offered on macOS.
    /// </summary>
    /// <remarks>
    /// It hangs off the same question the self-update asks - can this program
    /// replace itself - and the platform used to be implied in the answer.
    /// Teaching macOS to replace itself made that implication false, and the
    /// setup window would have offered to copy the program out of its bundle
    /// into a folder that only Windows has. Found by looking for what else
    /// reads that property.
    /// </remarks>
    /// <summary>Each platform is offered its own place, not the other's.</summary>
    /// <remarks>
    /// This test used to say "on Windows only" and assert that the target ends
    /// in <c>.exe</c> "whatever machine asks". Since 0.9.0 that is wrong on both
    /// counts, and only the macOS job in CI said so - on Windows the assertion
    /// stayed true and the release workflow, which tests on windows-latest,
    /// stayed green. The pitfall the repository already knew: **the machine that
    /// finds this kind of fault is the other one.**
    /// </remarks>
    [Fact]
    public void EachPlatformIsOfferedItsOwnPlace()
    {
        if (OperatingSystem.IsMacOS())
        {
            Assert.Equal("/Applications/ClaudeUsageChecker.app", SelfInstaller.TargetPath);
            Assert.DoesNotContain('\\', SelfInstaller.TargetPath);
        }
        else if (OperatingSystem.IsWindows())
        {
            Assert.EndsWith(".exe", SelfInstaller.TargetPath, StringComparison.Ordinal);
        }
        else
        {
            // Nowhere else can it replace itself, so nowhere else is it offered.
            Assert.False(SelfInstaller.ShouldOffer);
        }
    }

    /// <summary>
    /// A signing tool that is not there answers no, rather than throwing.
    /// </summary>
    /// <remarks>
    /// Process.Start throws when the program is missing, and the exception is
    /// of a kind the update path does not catch - it would travel up to the
    /// message loop and end the application without a word, which is the
    /// failure mode every tray action is guarded against. Answering no is also
    /// the right answer: what cannot be verified is not installed.
    ///
    /// On a Mac these programs are always there, so the test can only run where
    /// they are not - which is where it matters.
    /// </remarks>
    [Fact]
    public async Task AMissingSigningToolIsAnsweredRatherThanThrown()
    {
        if (OperatingSystem.IsMacOS())
        {
            return;
        }

        Assert.False(await MacOsBundle.IsAcceptableAsync("/Applications/X.app", CancellationToken.None));
    }

    /// <summary>
    /// The new version is unpacked beside the old one, not somewhere else.
    /// </summary>
    /// <remarks>
    /// Putting it in place is a rename, and a rename cannot cross volumes. The
    /// temporary folder usually sits on the same disk as the applications
    /// folder, so this would work almost everywhere and fail on an external
    /// one - at the last step of all, with everything downloaded, verified and
    /// the old version already set aside.
    /// </remarks>
    [Fact]
    public void TheNewVersionIsUnpackedBesideTheOldOne()
    {
        var workspace = MacOsBundle.WorkspaceBeside("/Volumes/Extern/ClaudeUsageChecker.app");

        Assert.Equal("/Volumes/Extern", AsMacOsPath(Path.GetDirectoryName(workspace)!));
        Assert.StartsWith("/Volumes/Extern/", AsMacOsPath(workspace), StringComparison.Ordinal);
    }

    /// <summary>Two updates at once do not share a folder.</summary>
    [Fact]
    public void EachUpdateGetsAFolderOfItsOwn() =>
        Assert.NotEqual(
            MacOsBundle.WorkspaceBeside("/Applications/ClaudeUsageChecker.app"),
            MacOsBundle.WorkspaceBeside("/Applications/ClaudeUsageChecker.app"));
}
