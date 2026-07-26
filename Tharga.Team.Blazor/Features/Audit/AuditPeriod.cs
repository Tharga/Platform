namespace Tharga.Team.Blazor.Features.Audit;

/// <summary>
/// Date arithmetic for the audit log's period filter.
/// </summary>
/// <remarks>
/// Pure and static so it is unit-testable — this project has no bUnit, so a calculation left inside the
/// component is unreachable from tests. Mirrors <c>TeamActionGate</c> and <c>TeamVisibility</c>.
/// </remarks>
internal static class AuditPeriod
{
    /// <summary>
    /// The Monday that starts <paramref name="day"/>'s week (ISO-8601).
    /// </summary>
    /// <remarks>
    /// Not <c>AddDays(-(int)DayOfWeek)</c>: that counts from Sunday, because
    /// <see cref="System.DayOfWeek.Sunday"/> is 0. On a Sunday it subtracts nothing, so "This week"
    /// silently collapses to "Today" and hides the six days the reader is asking for.
    /// </remarks>
    public static DateTime StartOfWeek(DateTime day)
        => day.Date.AddDays(-(((int)day.DayOfWeek + 6) % 7));
}
