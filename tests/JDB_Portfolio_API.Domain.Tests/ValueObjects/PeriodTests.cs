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
}
