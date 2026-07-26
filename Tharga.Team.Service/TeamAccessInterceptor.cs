using Tharga.MongoDB.Interception;

namespace Tharga.Team.Service;

/// <summary>
/// Refuses any database operation that no authorization decision covers.
/// </summary>
/// <remarks>
/// Defence in depth, not the primary control — <c>AddTeamService</c> / <c>AddSystemService</c> are what
/// authorize a call. This catches the case those cannot: code that reaches the database without going
/// through the authorization layer at all, including a consumer's own repositories, which Platform never
/// sees. Register it with:
/// <code>
/// builder.AddMongoDB(o => o.AddCollectionInterceptor&lt;TeamAccessInterceptor&gt;());
/// </code>
/// <para>
/// It runs at <see cref="InterceptionPoint.Invocation"/> only. At
/// <see cref="InterceptionPoint.Enumeration"/> a deferred operation's work happens when the consumer
/// enumerates, potentially long after the authorizing scope has disposed — checking there would reject
/// legitimate calls and push people towards widening scopes until the guard meant nothing.
/// </para>
/// <para>
/// It asserts that a decision was made, not that the right one was: at this layer the entity being
/// touched carries no team, so there is nothing to compare a team key against. Binding a scope to the
/// team named in the call is <c>ScopeProxy</c>'s job.
/// </para>
/// </remarks>
public sealed class TeamAccessInterceptor : ICollectionInterceptor
{
    public InterceptionPoint Points => InterceptionPoint.Invocation;

    public ValueTask<InterceptDecision> BeforeCallAsync(CollectionCallInfo call, CancellationToken cancellationToken = default)
    {
        var context = TeamAccess.Current;

        var decision = context == null
            ? InterceptDecision.Reject(
                $"No authorization covers this call. Register the service with AddTeamService/AddSystemService, " +
                $"or declare deliberate access with TeamAccess.System(reason) / TeamAccess.Unchecked(reason).")
            : InterceptDecision.Proceed;

        return ValueTask.FromResult(decision);
    }
}
