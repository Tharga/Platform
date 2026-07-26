using Tharga.Team.Blazor.Features.Audit;

namespace Tharga.Team.Blazor.Tests;

/// <summary>
/// The period filter reads back a fixed number of days from now. It replaced calendar "This week" /
/// "This month" options, whose week arithmetic counted from Sunday — so on a Sunday "This week"
/// subtracted nothing and silently showed only today, while "This month" showed the missing entries.
/// A rolling window has no week-start to get wrong.
/// </summary>
public class AuditPeriodTests
{
    private static readonly DateTime UtcNow = new(2026, 7, 26, 9, 30, 0, DateTimeKind.Utc);
    private static readonly DateTime LocalToday = new(2026, 7, 26, 0, 0, 0, DateTimeKind.Local);

    [Theory]
    [InlineData(AuditPeriod.SevenDays, 7)]
    [InlineData(AuditPeriod.ThirtyDays, 30)]
    [InlineData(AuditPeriod.NinetyDays, 90)]
    public void RollingWindow_CountsBackFromNow(string period, int days)
    {
        var from = AuditPeriod.ResolveFrom(period, UtcNow, LocalToday);

        Assert.Equal(UtcNow.AddDays(-days), from);
    }

    [Theory]
    [InlineData("2026-07-20")] // Monday
    [InlineData("2026-07-24")] // Friday
    [InlineData("2026-07-26")] // Sunday — the day the calendar-week bug surfaced
    public void RollingWindow_IsTheSameLengthOnEveryWeekday(string today)
    {
        var now = DateTime.SpecifyKind(DateTime.Parse(today).AddHours(9), DateTimeKind.Utc);

        var from = AuditPeriod.ResolveFrom(AuditPeriod.SevenDays, now, now.Date);

        Assert.Equal(7, (now - from!.Value).TotalDays);
    }

    [Fact]
    public void Today_IsTheReadersCalendarDay()
    {
        var from = AuditPeriod.ResolveFrom(AuditPeriod.Today, UtcNow, LocalToday);

        Assert.Equal(LocalToday.ToUniversalTime(), from);
    }

    [Fact]
    public void All_HasNoLowerBound()
    {
        Assert.Null(AuditPeriod.ResolveFrom(AuditPeriod.All, UtcNow, LocalToday));
    }

    [Fact]
    public void AnUnknownPeriod_HasNoLowerBound()
    {
        Assert.Null(AuditPeriod.ResolveFrom("whenever", UtcNow, LocalToday));
    }
}
