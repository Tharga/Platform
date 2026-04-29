using Tharga.MongoDB;

namespace Tharga.Team.MongoDB;

public interface ITeamRepository<TTeamEntity, TMember> : IRepository
    where TTeamEntity : TeamEntityBase<TMember>
    where TMember : TeamMemberBase
{
    IAsyncEnumerable<TTeamEntity> GetTeamsByUserAsync(string userKey);
    Task<TTeamEntity> GetAsync(string teamKey);
    Task AddAsync(TTeamEntity teamEntity);
    Task DeleteAsync(string teamKey);
    Task RenameAsync(string teamKey, string name);
    Task SetLastSeenAsync(string teamKey, string userKey, DateTime utcNow);
    Task AddMemberAsync(string teamKey, TMember member);
    Task RemoveMemberAsync(string teamKey, string userKey);
    Task SetMemberRoleAsync(string teamKey, string userKey, AccessLevel accessLevel);
    Task SetMemberTenantRolesAsync(string teamKey, string userKey, string[] tenantRoles);
    Task SetMemberScopeOverridesAsync(string teamKey, string userKey, string[] scopeOverrides);
    Task SetMemberNameAsync(string teamKey, string userKey, string name);
    Task<ITeam> SetInvitationResponseAsync(string teamKey, string userKey, string inviteKey, bool accept);
    Task SetConsentAsync(string teamKey, string[] consentedRoles);
    IAsyncEnumerable<TTeamEntity> GetTeamsByConsentAsync(string[] roles);
}