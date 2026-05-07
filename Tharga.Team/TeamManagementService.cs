namespace Tharga.Team;

/// <summary>
/// Delegates to <see cref="ITeamService"/> for all operations.
/// Scope enforcement is handled by <c>ScopeProxy&lt;T&gt;</c> in Tharga.Team.Service.
/// Generic methods (GetTeamsAsync, DeleteTeamAsync, RenameTeamAsync) call non-generic
/// internal versions since the proxy resolves the member type from the team data.
/// </summary>
public class TeamManagementService<TMember> : ITeamManagementService
    where TMember : class, ITeamMember
{
    private readonly ITeamService _inner;

    public TeamManagementService(ITeamService inner)
    {
        _inner = inner;
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
    public Task SetMemberLastSeenAsync(string teamKey) => _inner.SetMemberLastSeenAsync(teamKey);
    public Task SetInvitationResponseAsync(string teamKey, string userKey, string inviteCode, bool accept) => _inner.SetInvitationResponseAsync(teamKey, userKey, inviteCode, accept);
}
