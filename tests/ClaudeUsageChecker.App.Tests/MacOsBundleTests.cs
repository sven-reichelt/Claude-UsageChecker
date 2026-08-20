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
    [Fact]
    public void ThePermanentSetupIsOfferedOnWindowsOnly()
    {
        if (!OperatingSystem.IsWindows())
        {
            Assert.False(SelfInstaller.ShouldOffer);
        }

        // The path it would copy to is a Windows one, whatever machine asks.
        Assert.EndsWith(".exe", SelfInstaller.TargetPath, StringComparison.Ordinal);
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
}
