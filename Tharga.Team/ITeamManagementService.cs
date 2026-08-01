namespace Tharga.Team;

/// <summary>
/// The scope-checked entry point for team operations — <b>the interface a component, controller or MCP
/// provider should inject</b>. Every member carries a <c>[RequireScope]</c> attribute enforced by
/// <c>ScopeProxy</c>, so a caller lacking the scope is refused before the operation runs.
/// </summary>
/// <remarks>
/// <see cref="ITeamService"/> is the internal path beneath this one: it is the contract a host implements,
/// and its reads are deliberately unchecked so that framework code — building claims, revalidating a
/// circuit — can read without needing the very scopes it is in the middle of computing. Calling it from a
/// first-level surface bypasses authorization entirely, which is why the read methods below exist.
/// </remarks>
public interface ITeamManagementService
{
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

    /// <summary>One team and its members. Requires <c>team:read</c> on that team.</summary>
    [RequireScope(TeamScopes.Read)]
    Task<ITeam<TMember>> GetTeamAsync<TMember>(string teamKey) where TMember : ITeamMember;

    /// <summary>Team metadata without the roster. Requires <c>team:read</c> on that team.</summary>
    [RequireScope(TeamScopes.Read)]
    Task<ITeam> GetTeamByKeyAsync(string teamKey);

    /// <summary>The team's members. Requires <c>team:read</c> on that team.</summary>
    [RequireScope(TeamScopes.Read)]
    IAsyncEnumerable<ITeamMember> GetMembersAsync(string teamKey);

    /// <summary>One member of a team. Requires <c>team:read</c> on that team.</summary>
    [RequireScope(TeamScopes.Read)]
    Task<ITeamMember> GetTeamMemberAsync(string teamKey, string userKey);
}
