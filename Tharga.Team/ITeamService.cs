namespace Tharga.Team;

public interface ITeamService
{
    event EventHandler<TeamsListChangedEventArgs> TeamsListChangedEvent;
    event EventHandler<SelectTeamEventArgs> SelectTeamEvent;

    IAsyncEnumerable<ITeam> GetTeamsAsync();
    IAsyncEnumerable<ITeam<TMember>> GetTeamsAsync<TMember>() where TMember : ITeamMember;
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
    Task SetTeamConsentAsync(string teamKey, string[] consentedRoles);
    IAsyncEnumerable<ITeam> GetConsentedTeamsAsync(string[] userRoles);
}