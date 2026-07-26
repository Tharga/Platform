namespace Tharga.Team.Blazor.Features.Audit;

/// <summary>
/// The audit log's period filter: how far back the log is read.
/// </summary>
/// <remarks>
/// Rolling windows counted back from now, rather than calendar weeks and months. "This week" invited two
/// questions the reader should not have to answer — where the week starts, and whether a Sunday belongs
/// to the week ending or beginning — and got the first one wrong for six days out of seven. "7 days"
/// means the last seven days from this moment, on every day of the week and in every locale.
/// <para>
/// Pure and static so it is unit-testable — this project has no bUnit, so a calculation left inside the
/// component is unreachable from tests. Mirrors <c>TeamActionGate</c> and <c>TeamVisibility</c>.
/// </para>
/// </remarks>
internal static class AuditPeriod
{
    public const string Today = "today";
    public const string SevenDays = "7d";
    public const string ThirtyDays = "30d";
    public const string NinetyDays = "90d";
    public const string All = "all";

    /// <summary>
    /// The earliest timestamp the filter admits, or null for no lower bound.
    /// </summary>
    /// <param name="period">One of the period constants on this type.</param>
    /// <param name="utcNow">Now, in UTC — the anchor the rolling windows count back from.</param>
    /// <param name="localToday">
    /// Local midnight, for <see cref="Today"/> only. "Today" is the reader's calendar day, so it is the one
    /// option that must be resolved in their time zone rather than UTC.
    /// </param>
    public static DateTime? ResolveFrom(string period, DateTime utcNow, DateTime localToday) => period switch
    {
        Today => localToday.ToUniversalTime(),
        SevenDays => utcNow.AddDays(-7),
        ThirtyDays => utcNow.AddDays(-30),
        NinetyDays => utcNow.AddDays(-90),
        _ => null
    };
}
