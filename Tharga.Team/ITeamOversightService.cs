namespace Tharga.Team;

/// <summary>
/// Every team, regardless of membership — <b>the interface a component, controller or MCP provider
/// should inject</b> for cross-team discovery. Requires the <see cref="SystemTeamScopes.Read"/> system
/// scope.
/// </summary>
/// <remarks>
/// <b>Wholly system-wide</b>, which is why it is its own interface. <see cref="ITeamManagementService"/>
/// is wholly <i>team-bound</i> — every method names a team in its first argument, and that is what makes
/// one scope registration true of all of them. A no-argument cross-team read on that interface would
/// break the invariant rather than merely sit oddly beside it.
/// <para>
/// Distinct from <see cref="ITeamDirectoryService"/>, which is the caller's <i>own</i> teams filtered by
/// what each membership grants. This one answers a different question: what teams exist at all.
/// </para>
/// <para>
/// <b>Discovery only.</b> Holding <c>teams:read</c> grants nothing <i>inside</i> a team — selecting one
/// the caller is not a member of still yields only what that team consented to, and nothing if it has
/// consented to nothing.
/// </para>
/// </remarks>
public interface ITeamOversightService
{
    /// <summary>Every team, without rosters.</summary>
    IAsyncEnumerable<ITeam> GetAllTeamsAsync();

    /// <inheritdoc cref="GetAllTeamsAsync()"/>
    IAsyncEnumerable<ITeam<TMember>> GetAllTeamsAsync<TMember>() where TMember : ITeamMember;
}
