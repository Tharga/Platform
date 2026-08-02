namespace Tharga.Team.Service.Audit;

/// <summary>
/// Reading one team's audit log. <b>The interface every surface injects</b> — the Blazor view, the REST
/// endpoint and the MCP resource alike.
/// </summary>
/// <remarks>
/// Authorization is the attribute, enforced by <c>ScopeProxy</c> against the team named in the first
/// argument. No surface checks anything itself, which is the whole point: audit was the last part of the
/// toolkit still gated at its surfaces, and the three had already drifted — the UI and REST asked
/// <c>AuditAccess.CanRead</c> while MCP asked whether the caller held a host-configurable role, so the
/// same API key got different answers from different doors.
/// <para>
/// <b>Reading across every team lives on <see cref="IAuditOversightService"/>.</b> The split is not
/// stylistic: a team-bound service must name a team, so no call on this interface can reach past one.
/// Invariant I1 — <i>a team API key never reaches system-wide audit</i> — becomes a property of the shape
/// rather than a check somebody remembered to write.
/// </para>
/// </remarks>
public interface IAuditReadService
{
    /// <summary>
    /// Audit entries for <paramref name="teamKey"/>, newest first. Requires <c>audit:read</c> on that
    /// team — held directly, or through an access level the team consented to.
    /// </summary>
    /// <remarks>
    /// <paramref name="teamKey"/> is first because <c>ScopeProxy</c> resolves the target team from it.
    /// Any <see cref="AuditQuery.TeamKey"/> on <paramref name="query"/> is overwritten with it, so a
    /// caller authorized for one team cannot widen the query past the team they were checked against.
    /// </remarks>
    [RequireScope(AuditScopes.Read)]
    Task<AuditQueryResult> QueryAsync(string teamKey, AuditQuery query);
}
