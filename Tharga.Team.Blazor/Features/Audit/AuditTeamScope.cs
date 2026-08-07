namespace Tharga.Team.Blazor.Features.Audit;

/// <summary>
/// Which team an audit read is scoped to, given the three places a team can be named on
/// <c>AuditLogView</c>: the query being run, a pinned filter, and the component's own <c>TeamKey</c>
/// parameter.
/// </summary>
/// <remarks>
/// <b>Extracted so the precedence is asserted rather than described</b> (Tharga/Team#175). The parameter used
/// to be read when building a query but not when choosing which service authorized it, so a host passing
/// <c>TeamKey</c> alone was refused even holding that team's <c>audit:read</c> — the gate probe names no team
/// of its own, so it fell through to the system-scope branch. Nothing failed at build or in DI; the page
/// rendered "Access denied.", which reads as a missing grant.
/// </remarks>
internal static class AuditTeamScope
{
    /// <summary>
    /// The effective team, or null when the read is system-wide.
    /// </summary>
    /// <remarks>
    /// Order is most specific first. The query wins because <c>ApplyPinnedFilter</c> has already forced a
    /// pinned team onto it, so by the time a grid query is resolved the two agree; the pin is still consulted
    /// for reads built without going through that path, such as the access probe. The component parameter is
    /// last because it is the weakest statement of the three — a default scope rather than a decision about
    /// this particular read.
    /// </remarks>
    public static string Resolve(string queryTeamKey, string pinnedTeamKey, string parameterTeamKey)
    {
        if (!string.IsNullOrEmpty(queryTeamKey)) return queryTeamKey;
        if (!string.IsNullOrEmpty(pinnedTeamKey)) return pinnedTeamKey;
        return string.IsNullOrEmpty(parameterTeamKey) ? null : parameterTeamKey;
    }
}
