namespace Tharga.Team;

/// <summary>
/// Scope-enforced service for team management mutations.
/// Read operations use ITeamService directly.
/// </summary>
public interface ITeamManagementService
{
    [RequireScope(TeamScopes.Manage)]
    Task<ITeam> CreateTeamAsync(string name = null);

    [RequireScope(TeamScopes.Manage)]
    Task RenameTeamAsync(string teamKey, string name);

    [RequireScope(TeamScopes.Manage)]
    Task DeleteTeamAsync(string teamKey);

    [RequireScope(TeamScopes.MemberManage)]
    Task AddMemberAsync(string teamKey, InviteUserModel model);

    [RequireScope(TeamScopes.MemberManage)]
    Task RemoveMemberAsync(string teamKey, string userKey);

    [RequireScope(TeamScopes.MemberManage)]
    Task SetMemberRoleAsync(string teamKey, string userKey, AccessLevel accessLevel);

    [RequireScope(TeamScopes.MemberManage)]
    Task SetMemberTenantRolesAsync(string teamKey, string userKey, string[] tenantRoles);

    [RequireScope(TeamScopes.MemberManage)]
    Task SetMemberScopeOverridesAsync(string teamKey, string userKey, string[] scopeOverrides);

    [RequireScope(TeamScopes.Manage)]
    Task SetMemberNameAsync(string teamKey, string userKey, string name);

    [RequireScope(TeamScopes.Manage)]
    Task TransferOwnershipAsync(string teamKey, string newOwnerUserKey);

    [RequireScope(TeamScopes.Read)]
    Task SetMemberLastSeenAsync(string teamKey);

    [RequireScope(TeamScopes.Read)]
    Task SetInvitationResponseAsync(string teamKey, string userKey, string inviteCode, bool accept);
}
