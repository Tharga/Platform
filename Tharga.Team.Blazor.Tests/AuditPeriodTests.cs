using Tharga.Team.Blazor.Features.Audit;

namespace Tharga.Team.Blazor.Tests;

/// <summary>
/// "This week" must mean the whole week. The previous <c>AddDays(-(int)DayOfWeek)</c> counted from
/// Sunday, so on a Sunday it subtracted nothing and the filter silently showed only today — entries from
/// yesterday and the day before were missing, while "This month" showed them.
/// </summary>
public class AuditPeriodTests
{
    [Theory]
    // Every day of one ISO week resolves to the same Monday.
    [InlineData("2026-07-20", "2026-07-20")] // Monday
    [InlineData("2026-07-21", "2026-07-20")] // Tuesday
    [InlineData("2026-07-22", "2026-07-20")] // Wednesday
    [InlineData("2026-07-23", "2026-07-20")] // Thursday
    [InlineData("2026-07-24", "2026-07-20")] // Friday
    [InlineData("2026-07-25", "2026-07-20")] // Saturday
    [InlineData("2026-07-26", "2026-07-20")] // Sunday — the day the bug was reported
    public void StartOfWeek_IsTheMonday(string day, string expected)
    {
        Assert.Equal(DateTime.Parse(expected), AuditPeriod.StartOfWeek(DateTime.Parse(day)));
    }

    [Fact]
    public void StartOfWeek_OnSunday_LooksBackSixDays()
    {
        var sunday = new DateTime(2026, 7, 26);

        Assert.Equal(6, (sunday - AuditPeriod.StartOfWeek(sunday)).TotalDays);
    }

    [Fact]
    public void StartOfWeek_DiscardsTheTimeOfDay()
    {
        var sundayAfternoon = new DateTime(2026, 7, 26, 14, 30, 0);

        Assert.Equal(new DateTime(2026, 7, 20), AuditPeriod.StartOfWeek(sundayAfternoon));
    }

    [Fact]
    public void StartOfWeek_CrossesAMonthBoundary()
    {
        // Wednesday 1 July 2026 — the week starts in June.
        Assert.Equal(new DateTime(2026, 6, 29), AuditPeriod.StartOfWeek(new DateTime(2026, 7, 1)));
    }
}
