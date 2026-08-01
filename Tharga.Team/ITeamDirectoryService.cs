namespace Tharga.Team;

/// <summary>
/// The caller's own teams — <b>the interface a component, controller or MCP provider should inject</b> to
/// list them. Scope-<i>filtered</i> rather than scope-gated: each team is included only if the caller's
/// membership in that team grants <c>team:read</c>.
/// </summary>
/// <remarks>
/// Separate from <see cref="ITeamManagementService"/> because that interface is wholly team-bound — every
/// operation names a team in its first argument, which is what lets one registration authorize all of
/// them. This one names no team, so it cannot be gated the same way and does not belong there.
/// <para>
/// It cannot use <c>[RequireScope]</c> for the same reason, and a principal carries scope claims only for
/// the *selected* team, so there is nothing in the claims to check the others against. The scopes are
/// recomputed per team from the caller's membership instead — the same inputs the claims builder uses.
/// </para>
/// </remarks>
public interface ITeamDirectoryService
{
    /// <summary>
    /// The caller's teams, omitting any where their membership does not grant <c>team:read</c>. A team is
    /// omitted whole rather than returned without its roster: the scope covers "team details and members"
    /// together, so a half-visible team would be a state nothing else in the model has.
    /// </summary>
    IAsyncEnumerable<ITeam<TMember>> GetTeamsAsync<TMember>() where TMember : ITeamMember;
}
