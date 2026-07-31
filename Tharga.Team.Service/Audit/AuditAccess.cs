using System.Security.Claims;

namespace Tharga.Team.Service.Audit;

/// <summary>
/// Whether a caller may read the audit log, and for which team.
/// </summary>
/// <remarks>
/// Extracted so every surface — the Blazor view, the REST endpoint, and anything added later — asks the
/// same question of the same code. The rule restated per surface is the rule that drifts: three places
/// deciding "may this caller read audit" is three chances for one of them to be wrong, and the one that
/// is wrong is the one nobody tested.
/// </remarks>
public static class AuditAccess
{
    /// <summary>
    /// Whether <paramref name="principal"/> may read audit entries for <paramref name="teamKey"/>, or
    /// across every team when it is null.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>For one team:</b> <c>audit:read</c> held on that team, however the caller came by it — an
    /// access level, a tenant role, a scope override — or held system-wide.
    /// </para>
    /// <para>
    /// <b>Across all teams:</b> a system grant only. A team grant is issued for the selected team, so
    /// accepting it here would let a team administrator read every team's log — the hole the
    /// <c>Scope</c> / <c>SystemScope</c> provenance split closed.
    /// </para>
    /// </remarks>
    public static bool CanRead(ClaimsPrincipal principal, string teamKey)
    {
        if (principal == null) return false;

        if (string.IsNullOrEmpty(teamKey))
            return TeamScopePolicy.HasSystemScope(principal, AuditScopes.Read);

        return TeamScopePolicy.HasTeamScope(principal, AuditScopes.Read, teamKey)
               || TeamScopePolicy.HasSystemScope(principal, AuditScopes.Read);
    }
}
