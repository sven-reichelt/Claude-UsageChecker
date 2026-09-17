using ClaudeUsageChecker.App.Services;

namespace ClaudeUsageChecker.App.Tests;

/// <summary>
/// What becomes of a new version: installed at startup, announced, or asked
/// about in the background - and how often.
/// </summary>
public class UpdatePolicyTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 17, 10, 0, 0, TimeSpan.Zero);
    private static readonly ProgramVersion Next = new(new Version(1, 0, 2));
    private static readonly ProgramVersion AfterThat = new(new Version(1, 0, 3));

    [Fact]
    public void AtStartup_AutomaticUpdatesInstallWithoutAsking()
    {
        Assert.Equal(StartupUpdateAction.Install,
            UpdatePolicy.AtStartup(Available(), automatic: true, informAtStartup: false, canInstallHere: true));
    }

    /// <summary>Without automatic updates the startup says it is there, as it always has.</summary>
    [Fact]
    public void AtStartup_WithoutAutomaticUpdatesTheUserIsTold()
    {
        Assert.Equal(StartupUpdateAction.Inform,
            UpdatePolicy.AtStartup(Available(), automatic: false, informAtStartup: true, canInstallHere: true));
    }

    [Fact]
    public void AtStartup_WithBothSwitchedOffNothingHappens()
    {
        Assert.Equal(StartupUpdateAction.None,
            UpdatePolicy.AtStartup(Available(), automatic: false, informAtStartup: false, canInstallHere: true));
    }

    /// <summary>
    /// A copy that cannot replace itself - from the downloads folder, or a
    /// development build - still tells whoever wanted automatic updates.
    /// </summary>
    [Fact]
    public void AtStartup_AutomaticUpdatesThatCannotHappenHereStillInform()
    {
        Assert.Equal(StartupUpdateAction.Inform,
            UpdatePolicy.AtStartup(Available(), automatic: true, informAtStartup: false, canInstallHere: false));
    }

    /// <summary>Without a checksum nothing is installed - automatically least of all.</summary>
    [Fact]
    public void AtStartup_AReleaseWithoutItsChecksumIsNeverInstalled()
    {
        var incomplete = Available() with { ChecksumUrl = null };

        Assert.Equal(StartupUpdateAction.Inform,
            UpdatePolicy.AtStartup(incomplete, automatic: true, informAtStartup: false, canInstallHere: true));
    }

    /// <summary>A restart must not take away a notice that waits to be confirmed.</summary>
    [Fact]
    public void AtStartup_SomethingWaitingForTheUserHoldsTheInstallBack()
    {
        Assert.Equal(StartupUpdateAction.Inform,
            UpdatePolicy.AtStartup(Available(), automatic: true, informAtStartup: false, canInstallHere: true, busy: true));
    }

    [Theory]
    [InlineData(UpdateCheckStatus.UpToDate)]
    [InlineData(UpdateCheckStatus.Unavailable)]
    [InlineData(UpdateCheckStatus.Failed)]
    public void AtStartup_WithoutANewVersionNothingHappens(UpdateCheckStatus status)
    {
        var result = new UpdateCheckResult { Status = status };

        Assert.Equal(StartupUpdateAction.None,
            UpdatePolicy.AtStartup(result, automatic: true, informAtStartup: true, canInstallHere: true));
    }

    [Fact]
    public void Reminder_ANewVersionIsAskedAboutOnce()
    {
        var reminder = new UpdateReminder();

        var first = reminder.ShouldAsk(Next, Now);
        reminder.Asked(Next);
        var twoHoursLater = reminder.ShouldAsk(Next, Now.AddHours(2));
        var aWeekLater = reminder.ShouldAsk(Next, Now.AddDays(7));

        Assert.True(first);
        Assert.False(twoHoursLater);
        Assert.False(aWeekLater);
    }

    [Fact]
    public void Reminder_TomorrowMeansTomorrow()
    {
        var reminder = new UpdateReminder();
        reminder.RemindTomorrow(Next, Now);

        Assert.False(reminder.ShouldAsk(Next, Now.AddHours(23)));
        Assert.True(reminder.ShouldAsk(Next, Now.AddHours(24)));
    }

    /// <summary>A version newer still is news, whatever was said about the last one.</summary>
    [Fact]
    public void Reminder_ANewerVersionIsAskedAboutAgain()
    {
        var reminder = new UpdateReminder();
        reminder.Asked(Next);

        Assert.True(reminder.ShouldAsk(AfterThat, Now));
    }

    [Fact]
    public void Reminder_NoVersionIsNothingToAskAbout()
    {
        Assert.False(new UpdateReminder().ShouldAsk(null, Now));
    }

    [Fact]
    public void TheBackgroundCheckRunsEveryTwoHours()
    {
        Assert.Equal(TimeSpan.FromHours(2), UpdatePolicy.BackgroundInterval);
    }

    private static UpdateCheckResult Available() => new()
    {
        Status = UpdateCheckStatus.UpdateAvailable,
        AvailableVersion = Next,
        DownloadUrl = new Uri("https://example.invalid/ClaudeUsageChecker.exe"),
        ChecksumUrl = new Uri("https://example.invalid/ClaudeUsageChecker.exe.sha256"),
        Message = "1.0.2"
    };
}
