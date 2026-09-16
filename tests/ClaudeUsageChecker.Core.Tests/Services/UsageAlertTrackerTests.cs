using ClaudeUsageChecker.Core.Models;
using ClaudeUsageChecker.Core.Services;

namespace ClaudeUsageChecker.Core.Tests.Services;

/// <summary>
/// When a notice about a limit is due, and above all when it is not.
/// </summary>
/// <remarks>
/// The notice has to be confirmed. One that comes too often is worse than none:
/// after the third unnecessary one it is clicked away unread.
/// </remarks>
public class UsageAlertTrackerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 16, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset SessionReset = Now.AddHours(3);
    private static readonly DateTimeOffset WeeklyReset = Now.AddDays(4);

    private static readonly UsageAlertRules Rules = new(WarningThreshold: 75, CriticalThreshold: 90);

    [Fact]
    public void Evaluate_BelowTheWarningThresholdNothingIsReported()
    {
        var tracker = new UsageAlertTracker();

        var alerts = tracker.Evaluate(State(session: 74), Rules, Now, out _);

        Assert.Empty(alerts);
    }

    [Fact]
    public void Evaluate_CrossingTheWarningThresholdReportsOnce()
    {
        var tracker = new UsageAlertTracker();

        var first = tracker.Evaluate(State(session: 76), Rules, Now, out _);
        var second = tracker.Evaluate(State(session: 80), Rules, Now.AddMinutes(5), out _);

        var alert = Assert.Single(first);
        Assert.Equal(UsageAlertLevel.Warning, alert.Level);
        Assert.Equal(UsageAlertLimit.Session, alert.Limit);
        Assert.Equal(75d, alert.Threshold);
        Assert.Equal(SessionReset, alert.Window.ResetsAt);
        Assert.Empty(second);
    }

    [Fact]
    public void Evaluate_FromYellowToRedReportsAgain()
    {
        var tracker = new UsageAlertTracker();
        tracker.Evaluate(State(session: 76), Rules, Now, out _);

        var alerts = tracker.Evaluate(State(session: 91), Rules, Now.AddMinutes(5), out _);

        Assert.Equal(UsageAlertLevel.Critical, Assert.Single(alerts).Level);
    }

    [Fact]
    public void Evaluate_ReachingOneHundredReportsTheLimitAsUsedUp()
    {
        var tracker = new UsageAlertTracker();
        tracker.Evaluate(State(session: 91), Rules, Now, out _);

        var alerts = tracker.Evaluate(State(session: 100), Rules, Now.AddMinutes(5), out _);

        var alert = Assert.Single(alerts);
        Assert.Equal(UsageAlertLevel.Exhausted, alert.Level);
        Assert.Equal(100d, alert.Threshold);
    }

    /// <summary>
    /// A limit that skips a stage gets one notice about the highest, not two.
    /// </summary>
    [Fact]
    public void Evaluate_JumpingStraightToRedReportsOnlyRed()
    {
        var tracker = new UsageAlertTracker();

        var alerts = tracker.Evaluate(State(session: 95), Rules, Now, out _);

        Assert.Equal(UsageAlertLevel.Critical, Assert.Single(alerts).Level);
    }

    [Fact]
    public void Evaluate_ADisabledStageStaysQuietButIsRemembered()
    {
        var tracker = new UsageAlertTracker();
        var rules = Rules with { OnWarning = false };

        var atYellow = tracker.Evaluate(State(session: 80), rules, Now, out _);
        var atRed = tracker.Evaluate(State(session: 92), rules, Now.AddMinutes(5), out _);

        Assert.Empty(atYellow);
        Assert.Equal(UsageAlertLevel.Critical, Assert.Single(atRed).Level);
    }

    /// <summary>
    /// Whoever asked to hear about yellow should not miss it because the limit
    /// went straight on to red, where they asked for nothing.
    /// </summary>
    [Fact]
    public void Evaluate_SkippingPastADisabledStageFallsBackToTheEnabledOne()
    {
        var tracker = new UsageAlertTracker();
        var rules = Rules with { OnCritical = false };

        var alerts = tracker.Evaluate(State(session: 95), rules, Now, out _);

        var alert = Assert.Single(alerts);
        Assert.Equal(UsageAlertLevel.Warning, alert.Level);
        Assert.Equal(75d, alert.Threshold);
    }

    [Fact]
    public void Evaluate_WithEverythingSwitchedOffNothingIsReported()
    {
        var tracker = new UsageAlertTracker();
        var rules = Rules with { OnWarning = false, OnCritical = false, OnExhausted = false };

        var alerts = tracker.Evaluate(State(session: 100, weekly: 95), rules, Now, out _);

        Assert.Empty(alerts);
    }

    /// <summary>
    /// With the critical threshold at 100, red and used up are the same moment -
    /// and used up is what it is.
    /// </summary>
    [Fact]
    public void Evaluate_UsedUpWinsOverRedAtTheSameMoment()
    {
        var tracker = new UsageAlertTracker();
        var rules = new UsageAlertRules(WarningThreshold: 80, CriticalThreshold: 100);

        var alerts = tracker.Evaluate(State(session: 100), rules, Now, out _);

        Assert.Equal(UsageAlertLevel.Exhausted, Assert.Single(alerts).Level);
    }

    [Fact]
    public void Evaluate_EveryLimitIsFollowedOnItsOwn()
    {
        var tracker = new UsageAlertTracker();
        tracker.Evaluate(State(session: 10, weekly: 80), Rules, Now, out _);

        // The weekly limit keeps the icon yellow; the session joining it is
        // still news.
        var alerts = tracker.Evaluate(State(session: 77, weekly: 81), Rules, Now.AddMinutes(5), out _);

        var alert = Assert.Single(alerts);
        Assert.Equal(UsageAlertLimit.Session, alert.Limit);
    }

    [Fact]
    public void Evaluate_ModelSpecificLimitsCarryTheirName()
    {
        var tracker = new UsageAlertTracker();
        var snapshot = Snapshot(session: 5, weekly: 20) with
        {
            ScopedWeekly = [new ScopedUsageWindow("Fable", new UsageWindow(93, WeeklyReset))]
        };

        var alerts = tracker.Evaluate(Ready(snapshot), Rules, Now, out _);

        var alert = Assert.Single(alerts);
        Assert.Equal(UsageAlertLimit.WeeklyModel, alert.Limit);
        Assert.Equal("Fable", alert.ModelName);
        Assert.Equal("weekly:Fable", alert.Key);
    }

    /// <summary>
    /// After the reset the limit has started over, and reaching yellow in the new
    /// period is news again.
    /// </summary>
    [Fact]
    public void Evaluate_AfterTheResetTheNextCrossingIsReportedAgain()
    {
        var tracker = new UsageAlertTracker();
        tracker.Evaluate(State(session: 80), Rules, Now, out _);

        var later = SessionReset.AddMinutes(10);
        var nextReset = SessionReset.AddHours(5);
        var snapshot = Snapshot(session: 0, weekly: 20) with { Session = new UsageWindow(78, nextReset) };

        var alerts = tracker.Evaluate(Ready(snapshot), Rules, later, out _);

        Assert.Equal(UsageAlertLevel.Warning, Assert.Single(alerts).Level);
    }

    /// <summary>
    /// Figures whose reset is already due belong to the period that has ended.
    /// </summary>
    /// <remarks>
    /// They linger until the next call. Judged as they are, the ended period
    /// would be reported once more as if it were the new one.
    /// </remarks>
    [Fact]
    public void Evaluate_FiguresWhoseResetIsDueAreLeftAlone()
    {
        var tracker = new UsageAlertTracker();
        tracker.Evaluate(State(session: 80), Rules, Now, out _);

        var alerts = tracker.Evaluate(State(session: 80), Rules, SessionReset.AddMinutes(1), out _);

        Assert.Empty(alerts);
    }

    [Fact]
    public void Evaluate_RaisingTheThresholdsArmsTheNoticeAgain()
    {
        var tracker = new UsageAlertTracker();
        tracker.Evaluate(State(session: 80), Rules, Now, out _);

        var raised = new UsageAlertRules(WarningThreshold: 85, CriticalThreshold: 95);
        var afterRaising = tracker.Evaluate(State(session: 80), raised, Now.AddMinutes(1), out _);
        var atTheNewThreshold = tracker.Evaluate(State(session: 86), raised, Now.AddMinutes(5), out _);

        Assert.Empty(afterRaising);
        Assert.Equal(UsageAlertLevel.Warning, Assert.Single(atTheNewThreshold).Level);
    }

    /// <summary>
    /// Lowering the thresholds below the current figure is how the notice is
    /// tried out - it has to appear straight away.
    /// </summary>
    [Fact]
    public void Evaluate_LoweringTheThresholdsBelowTheFigureReportsAtOnce()
    {
        var tracker = new UsageAlertTracker();
        tracker.Evaluate(State(session: 30), Rules, Now, out _);

        var lowered = new UsageAlertRules(WarningThreshold: 25, CriticalThreshold: 40);
        var alerts = tracker.Evaluate(State(session: 30), lowered, Now.AddMinutes(1), out _);

        Assert.Equal(UsageAlertLevel.Warning, Assert.Single(alerts).Level);
    }

    [Theory]
    [InlineData(UsageStateKind.Stale)]
    [InlineData(UsageStateKind.Initializing)]
    [InlineData(UsageStateKind.Unavailable)]
    [InlineData(UsageStateKind.AuthenticationFailed)]
    public void Evaluate_OnlyFreshFiguresAreJudged(UsageStateKind kind)
    {
        var tracker = new UsageAlertTracker();
        var state = State(session: 95) with { Kind = kind };

        var alerts = tracker.Evaluate(state, Rules, Now, out _);

        Assert.Empty(alerts);
    }

    /// <summary>
    /// An autostart every morning must not repeat what was said the day before.
    /// </summary>
    [Fact]
    public void Evaluate_WhatWasReportedSurvivesARestart()
    {
        var before = new UsageAlertTracker();
        before.Evaluate(State(session: 10, weekly: 80), Rules, Now, out _);

        var after = new UsageAlertTracker(before.Marks);
        var alerts = after.Evaluate(State(session: 10, weekly: 82), Rules, Now.AddHours(20), out _);

        Assert.Empty(alerts);
    }

    [Fact]
    public void Evaluate_OnlyARealChangeAsksToBeWrittenDown()
    {
        var tracker = new UsageAlertTracker();
        tracker.Evaluate(State(session: 76), Rules, Now, out var afterTheFirst);

        // The API reports the reset with fractions of a second that differ from
        // call to call; that alone is no reason to write a file.
        var jittered = Snapshot(session: 77, weekly: 20) with
        {
            Session = new UsageWindow(77, SessionReset.AddMilliseconds(420))
        };
        tracker.Evaluate(Ready(jittered), Rules, Now.AddMinutes(5), out var afterTheSame);

        Assert.True(afterTheFirst);
        Assert.False(afterTheSame);
    }

    [Fact]
    public void Evaluate_MarksOfEndedPeriodsAreForgotten()
    {
        var tracker = new UsageAlertTracker();
        tracker.Evaluate(State(session: 80), Rules, Now, out _);

        tracker.Evaluate(new UsageState { Kind = UsageStateKind.Unavailable }, Rules, SessionReset.AddHours(1), out var changed);

        Assert.True(changed);
        Assert.False(tracker.Marks.ContainsKey("session"));
        Assert.True(tracker.Marks.ContainsKey("weekly"));
    }

    private static UsageState State(double session, double weekly = 20) => Ready(Snapshot(session, weekly));

    private static UsageState Ready(UsageSnapshot snapshot) => new()
    {
        Kind = UsageStateKind.Ready,
        Snapshot = snapshot
    };

    private static UsageSnapshot Snapshot(double session, double weekly) => new()
    {
        Session = new UsageWindow(session, SessionReset),
        Weekly = new UsageWindow(weekly, WeeklyReset),
        RetrievedAt = Now
    };
}
