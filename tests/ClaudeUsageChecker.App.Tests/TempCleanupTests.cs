using ClaudeUsageChecker.App.Services;

namespace ClaudeUsageChecker.App.Tests;

/// <summary>
/// Checks the restraint of the cleanup.
/// </summary>
/// <remarks>
/// A cleaner that deletes too much is worse than none at all - it would remove
/// the extraction of a running version. In a development build, where the folder
/// in use cannot be determined, nothing is touched on purpose.
/// </remarks>
public class TempCleanupTests
{
    [Fact]
    public void WithoutARecognisableOwnFolderNothingIsDeleted()
    {
        // Im Entwicklungsstand liegt keine Entpackung vor, es laedt also kein
        // module from there. That removes any basis for deciding which folder is
        // in use - and then nothing is touched, on purpose.
        Assert.Equal(0, TempCleanup.RemoveStaleExtractions());
    }

    [Fact]
    public void TheCleanupLeavesTheTempDirectoryUntouched()
    {
        var basis = Path.Combine(Path.GetTempPath(), ".net");
        var vorher = Directory.Exists(basis) ? Directory.GetDirectories(basis).Length : -1;

        TempCleanup.RemoveStaleExtractions();

        var nachher = Directory.Exists(basis) ? Directory.GetDirectories(basis).Length : -1;
        Assert.Equal(vorher, nachher);
    }
}
