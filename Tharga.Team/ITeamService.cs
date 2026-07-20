namespace Tharga.Team;

public interface ITeamService
{
    event EventHandler<TeamsListChangedEventArgs> TeamsListChangedEvent;
    event EventHandler<SelectTeamEventArgs> SelectTeamEvent;

    IAsyncEnumerable<ITeam> GetTeamsAsync();
    IAsyncEnumerable<ITeam<TMember>> GetTeamsAsync<TMember>() where TMember : ITeamMember;

    /// <summary>
    /// Every team, regardless of membership. Requires the <see cref="SystemTeamScopes.Read"/> system scope.
    /// </summary>
    /// <remarks>
    /// Discovery only — the returned teams carry no implied access. Acting inside a team the caller is not
    /// a member of still depends on that team's consent. Use <see cref="GetTeamsAsync()"/> for the caller's
    /// own teams; this method is for oversight surfaces (support, administration).
    /// </remarks>
    IAsyncEnumerable<ITeam> GetAllTeamsAsync();

    /// <inheritdoc cref="GetAllTeamsAsync()"/>
    IAsyncEnumerable<ITeam<TMember>> GetAllTeamsAsync<TMember>() where TMember : ITeamMember;
    Task<ITeam<TMember>> GetTeamAsync<TMember>(string teamKey) where TMember : ITeamMember;
    Task<ITeam> CreateTeamAsync(string name = null);
    Task RenameTeamAsync<TMember>(string teamKey, string name) where TMember : ITeamMember;
    Task DeleteTeamAsync<TMember>(string teamKey) where TMember : ITeamMember;
    Task<ITeamMember> GetTeamMemberAsync(string teamKey, string userKey);
    IAsyncEnumerable<ITeamMember> GetMembersAsync(string teamKey);
    Task AddMemberAsync(string teamKey, InviteUserModel model);
    Task RemoveMemberAsync(string teamKey, string userKey);
    Task SetMemberRoleAsync(string teamKey, string userKey, AccessLevel accessLevel);
    Task SetMemberTenantRolesAsync(string teamKey, string userKey, string[] tenantRoles);
    Task SetMemberScopeOverridesAsync(string teamKey, string userKey, string[] scopeOverrides);
    Task SetMemberNameAsync(string teamKey, string userKey, string name);
    Task SetInvitationResponseAsync(string teamKey, string userKey, string inviteCode, bool accept);
    Task SetMemberLastSeenAsync(string teamKey);
    Task TransferOwnershipAsync<TMember>(string teamKey, string newOwnerUserKey) where TMember : ITeamMember;
    Task SetTeamConsentAsync(string teamKey, string[] consentedRoles, AccessLevel? accessLevel = null);
    IAsyncEnumerable<ITeam> GetConsentedTeamsAsync(string[] userRoles);
    Task<IReadOnlyList<TenantRoleDefinition>> GetTeamCustomRolesAsync(string teamKey);
    Task SetTeamCustomRolesAsync(string teamKey, IReadOnlyList<TenantRoleDefinition> customRoles);
}