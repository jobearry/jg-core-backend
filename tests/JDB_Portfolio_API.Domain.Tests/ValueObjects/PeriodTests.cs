using JDB_Portfolio_API.Domain.ValueObjects;

namespace JDB_Portfolio_API.Domain.Tests.ValueObjects;

public class PeriodTests
{
    private static readonly DateTime StartedAt = new(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime FinishedAt = new(2021, 6, 30, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Constructor_WithNoDates_LeavesBothDatesUnset()
    {
        var period = new Period();

        Assert.Null(period.StartedAt);
        Assert.Null(period.FinishedAt);
    }

    [Fact]
    public void Duration_WhenNoStartDate_IsNull()
    {
        var period = new Period();

        Assert.Null(period.Duration);
    }

    [Fact]
    public void Duration_WhenFinishedAtIsSet_StopsAtFinishedAt()
    {
        var period = new Period(StartedAt, FinishedAt);

        Assert.Equal(FinishedAt - StartedAt, period.Duration);
    }

    [Fact]
    public void Duration_WhenFinishedAtIsNotSet_IsMeasuredAgainstNow()
    {
        var period = new Period(DateTime.UtcNow.AddDays(-10), null);

        Assert.NotNull(period.Duration);
        Assert.InRange(period.Duration!.Value.TotalDays, 10, 10.05);
    }

    [Fact]
    public void Constructor_WithFinishedAtBeforeStartedAt_Throws()
    {
        Assert.Throws<ArgumentException>(() => new Period(StartedAt, StartedAt.AddDays(-1)));
    }

    [Fact]
    public void Constructor_WithDefaultStartedAt_Throws()
    {
        Assert.Throws<ArgumentException>(() => new Period(default(DateTime), null));
    }

    [Fact]
    public void Constructor_WithDefaultFinishedAt_Throws()
    {
        Assert.Throws<ArgumentException>(() => new Period(StartedAt, default(DateTime)));
    }

    /// <summary>
    /// Plan 3.1 — the other invariant. A <c>Period</c> records something that happened,
    /// so a start date in the future is not a period yet. Compared by date, so the
    /// time of day can never trip it.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(365)]
    public void Constructor_WithStartedAtInTheFuture_Throws(int daysAhead)
    {
        Assert.Throws<ArgumentException>(() => new Period(DateTime.UtcNow.AddDays(daysAhead), null));
    }

    [Fact]
    public void Constructor_WithStartedAtOfToday_IsAccepted()
    {
        var period = new Period(DateTime.UtcNow.Date, null);

        Assert.Equal(DateTime.UtcNow.Date, period.StartedAt);
    }

    /// <summary>
    /// Plan 3.1 — what <c>FinishedAt == null</c> means, as a named concept instead of
    /// folklore. It means "ongoing", but only once there is a start date; with no dates
    /// at all nothing is known, and the role is not ongoing.
    /// </summary>
    [Fact]
    public void IsOngoing_WhenFinishedAtIsUnsetAndStartedAtIsSet_IsTrue()
    {
        var period = new Period(StartedAt, null);

        Assert.True(period.IsOngoing);
    }

    [Fact]
    public void IsOngoing_WhenFinishedAtIsSet_IsFalse()
    {
        var period = new Period(StartedAt, FinishedAt);

        Assert.False(period.IsOngoing);
    }

    [Fact]
    public void IsOngoing_WhenNoDatesAreKnown_IsFalse()
    {
        Assert.False(new Period().IsOngoing);
    }

    [Fact]
    public void TwoPeriodsWithTheSameDates_AreEqual()
    {
        Assert.Equal(new Period(StartedAt, FinishedAt), new Period(StartedAt, FinishedAt));
        Assert.True(new Period(StartedAt) == new Period(StartedAt));
        Assert.NotEqual(new Period(StartedAt, FinishedAt), new Period(StartedAt, null));
    }

    [Fact]
    public void TwoPeriodsWithTheSameDates_ShareAHashCode()
    {
        Assert.Equal(new Period(StartedAt, FinishedAt).GetHashCode(),
                     new Period(StartedAt, FinishedAt).GetHashCode());
    }
}
