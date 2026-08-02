namespace Tharga.Team.Service.Audit;

/// <summary>
/// Reading audit across <b>every</b> team. Requires a <i>system</i> <c>audit:read</c> grant.
/// </summary>
/// <remarks>
/// Separate from <see cref="IAuditReadService"/> because the two ask different questions of the same
/// scope name. A team grant is issued for one team; accepting it here would let a team administrator read
/// every team's log, which is the hole the <c>Scope</c> / <c>SystemScope</c> provenance split closed.
/// <para>
/// Registered with <c>AddSystemService</c>, so <c>ScopeProxy</c> requires the system grant and never
/// consults a team one. There is no argument naming a team, so there is nothing here for a team-bound
/// caller to aim at — the confinement is structural.
/// </para>
/// <para>
/// This mirrors <c>ITeamManagementService</c> / <c>ITeamOversightService</c>, which split for exactly the
/// same reason one level up.
/// </para>
/// </remarks>
public interface IAuditOversightService
{
    /// <summary>
    /// Audit entries across every team, newest first. A <c>TeamKey</c> on the query narrows the result as
    /// a filter — the caller is authorized for all teams either way.
    /// </summary>
    [RequireScope(AuditScopes.Read)]
    Task<AuditQueryResult> QueryAllAsync(AuditQuery query);
}
