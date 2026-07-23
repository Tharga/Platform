namespace Tharga.Team;

/// <summary>
/// Team management mutations. Authorization is enforced in the service layer by
/// <c>AuthorizationTeamServiceDecorator</c> (over <see cref="ITeamService"/>); the <c>[RequireScope]</c>
/// attributes here document the scope each operation requires.
/// Read operations use ITeamService directly.
/// </summary>
public interface ITeamManagementService
{
    /// <summary>Create a team. Gated by <c>AllowTeamCreation</c> + authentication (no scope) — self-service.</summary>
    Task<ITeam> CreateTeamAsync(string name = null);

    [RequireScope(TeamScopes.Manage)]
    Task RenameTeamAsync(string teamKey, string name);

    /// <summary>Delete a team. Requires <c>team:manage</c> on the team (with <c>AllowTeamCreation</c>) or the <c>teams:delete</c> system scope.</summary>
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

    [RequireScope(TeamScopes.MemberManage)]
    Task SetMemberNameAsync(string teamKey, string userKey, string name);

    [RequireScope(TeamScopes.Manage)]
    Task TransferOwnershipAsync(string teamKey, string newOwnerUserKey);

    [RequireScope(TeamScopes.Manage)]
    Task SetTeamIconAsync(string teamKey, byte[] data, string contentType);

    [RequireScope(TeamScopes.Manage)]
    Task ClearTeamIconAsync(string teamKey);

    /// <summary>
    /// Replace the team's runtime-defined custom roles. Requires <c>team:manage</c> on the team. Each
    /// role's scopes must be app-registered scopes (rejected otherwise, as a privilege-escalation guard).
    /// Assigning these roles to members remains a <c>member:manage</c> operation.
    /// </summary>
    [RequireScope(TeamScopes.Manage)]
    Task SetTeamCustomRolesAsync(string teamKey, IReadOnlyList<TenantRoleDefinition> customRoles);

    [RequireScope(TeamScopes.Read)]
    Task SetMemberLastSeenAsync(string teamKey);

    [RequireScope(TeamScopes.Read)]
    Task SetInvitationResponseAsync(string teamKey, string userKey, string inviteCode, bool accept);
}
