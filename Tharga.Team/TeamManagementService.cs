namespace Tharga.Team;

/// <summary>
/// Delegates to <see cref="ITeamService"/> for all operations.
/// Scope enforcement is handled by <c>ScopeProxy&lt;T&gt;</c> in Tharga.Team.Service.
/// Generic methods (GetTeamsAsync, DeleteTeamAsync, RenameTeamAsync) call non-generic
/// internal versions since the proxy resolves the member type from the team data.
/// </summary>
public class TeamManagementService<TMember> : ITeamManagementService, ITeamLifecycleService, ITeamDirectoryService
    where TMember : class, ITeamMember
{
    private readonly ITeamService _inner;
    private readonly IUserService _userService;
    private readonly IScopeRegistry _scopeRegistry;

    public TeamManagementService(ITeamService inner)
        : this(inner, null, null)
    {
    }

    /// <summary>
    /// Preferred by the container when scopes are configured, so <see cref="GetTeamsAsync{T}"/> can filter
    /// per team. Falls back to the single-argument constructor when no <see cref="IScopeRegistry"/> is
    /// registered — an app not using scopes must not start refusing reads.
    /// </summary>
    public TeamManagementService(ITeamService inner, IUserService userService, IScopeRegistry scopeRegistry)
    {
        _inner = inner;
        _userService = userService;
        _scopeRegistry = scopeRegistry;
    }

    public Task<ITeam> CreateTeamAsync(string name = null) => _inner.CreateTeamAsync(name);
    public Task RenameTeamAsync(string teamKey, string name) => _inner.RenameTeamAsync<TMember>(teamKey, name);
    public Task DeleteTeamAsync(string teamKey) => _inner.DeleteTeamAsync<TMember>(teamKey);
    public Task AddMemberAsync(string teamKey, InviteUserModel model) => _inner.AddMemberAsync(teamKey, model);
    public Task RemoveMemberAsync(string teamKey, string userKey) => _inner.RemoveMemberAsync(teamKey, userKey);
    public Task SetMemberRoleAsync(string teamKey, string userKey, AccessLevel accessLevel) => _inner.SetMemberRoleAsync(teamKey, userKey, accessLevel);
    public Task SetMemberTenantRolesAsync(string teamKey, string userKey, string[] tenantRoles) => _inner.SetMemberTenantRolesAsync(teamKey, userKey, tenantRoles);
    public Task SetMemberScopeOverridesAsync(string teamKey, string userKey, string[] scopeOverrides) => _inner.SetMemberScopeOverridesAsync(teamKey, userKey, scopeOverrides);
    public Task SetMemberNameAsync(string teamKey, string userKey, string name) => _inner.SetMemberNameAsync(teamKey, userKey, name);
    public Task TransferOwnershipAsync(string teamKey, string newOwnerUserKey) => _inner.TransferOwnershipAsync<TMember>(teamKey, newOwnerUserKey);
    public Task SetTeamIconAsync(string teamKey, byte[] data, string contentType) => _inner.SetTeamIconAsync(teamKey, data, contentType);
    public Task ClearTeamIconAsync(string teamKey) => _inner.ClearTeamIconAsync(teamKey);
    public Task SetTeamCustomRolesAsync(string teamKey, IReadOnlyList<TenantRoleDefinition> customRoles) => _inner.SetTeamCustomRolesAsync(teamKey, customRoles);
    public Task SetMemberLastSeenAsync(string teamKey) => _inner.SetMemberLastSeenAsync(teamKey);
    public Task SetInvitationResponseAsync(string teamKey, string userKey, string inviteCode, bool accept) => _inner.SetInvitationResponseAsync(teamKey, userKey, inviteCode, accept);

    /// <summary>
    /// The caller's own teams, filtered to those where their membership grants <c>team:read</c>.
    /// </summary>
    /// <remarks>
    /// This one cannot carry <c>[RequireScope]</c>: it names no team, and <c>ScopeProxy</c> takes the team
    /// from the first argument. A principal also only ever holds scope claims for the *selected* team, so
    /// there is nothing in the claims to check the others against.
    /// <para>
    /// So the scopes are recomputed per team from the caller's membership in that team — the same inputs
    /// the claims builder uses. A team whose membership does not grant <c>team:read</c> is omitted rather
    /// than returned without its roster: the scope covers "team details and members" together, and a
    /// half-visible team would be a third state nothing else in the model has.
    /// </para>
    /// </remarks>
    public async IAsyncEnumerable<ITeam<T>> GetTeamsAsync<T>() where T : ITeamMember
    {
        var user = _userService == null ? null : await _userService.GetCurrentUserAsync();

        await foreach (var team in _inner.GetTeamsAsync<T>())
        {
            if (GrantsTeamRead(team, user)) yield return team;
        }
    }

    private bool GrantsTeamRead<T>(ITeam<T> team, IUser user) where T : ITeamMember
    {
        // No registry means the app does not use scopes; filtering here would refuse reads it never gated.
        if (_scopeRegistry == null || user == null) return true;

        var members = team.Members;
        if (members == null) return false;

        var member = members.Where(x => x.Key == user.Key).Select(x => (ITeamMember)x).FirstOrDefault();
        if (member == null) return false;

        return _scopeRegistry
            .GetEffectiveScopes(member.AccessLevel, member.TenantRoles, member.ScopeOverrides)
            .Contains(TeamScopes.Read);
    }

    public Task<ITeam<T>> GetTeamAsync<T>(string teamKey) where T : ITeamMember => _inner.GetTeamAsync<T>(teamKey);
    public Task<ITeam> GetTeamByKeyAsync(string teamKey) => _inner.GetTeamByKeyAsync(teamKey);
    public IAsyncEnumerable<ITeamMember> GetMembersAsync(string teamKey) => _inner.GetMembersAsync(teamKey);
    public Task<ITeamMember> GetTeamMemberAsync(string teamKey, string userKey) => _inner.GetTeamMemberAsync(teamKey, userKey);
}
