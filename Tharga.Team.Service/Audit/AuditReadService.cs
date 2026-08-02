namespace Tharga.Team.Service.Audit;

/// <summary>
/// Both audit read services, over <see cref="CompositeAuditLogger"/>.
/// </summary>
/// <remarks>
/// One class implementing both interfaces, registered twice — once team-bound, once system — so
/// <c>ScopeProxy</c> applies a different rule to each. The interfaces carry the authorization difference;
/// there is no behavioural difference below them worth a second type.
/// </remarks>
public sealed class AuditReadService(CompositeAuditLogger auditLogger) : IAuditReadService, IAuditOversightService
{
    /// <remarks>
    /// The team is taken from the argument the caller was authorized against, never from the query. A
    /// query naming a different team would otherwise read a team the scope check never saw.
    /// </remarks>
    public Task<AuditQueryResult> QueryAsync(string teamKey, AuditQuery query)
    {
        return auditLogger.QueryAsync((query ?? new AuditQuery()) with { TeamKey = teamKey });
    }

    /// <remarks>
    /// A <see cref="AuditQuery.TeamKey"/> on the query is honoured as a <i>filter</i>. The caller is
    /// already authorized across every team, so narrowing to one is not an authorization decision — and
    /// refusing it would force them to fetch every team and filter client-side, which is worse in every
    /// respect.
    /// </remarks>
    public Task<AuditQueryResult> QueryAllAsync(AuditQuery query)
    {
        return auditLogger.QueryAsync(query ?? new AuditQuery());
    }
}
