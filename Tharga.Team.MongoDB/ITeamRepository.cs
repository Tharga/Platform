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
    Task SetIconAsync(string teamKey, string reference);
    Task SetLastSeenAsync(string teamKey, string userKey, DateTime utcNow);
    Task AddMemberAsync(string teamKey, TMember member);
    Task RemoveMemberAsync(string teamKey, string userKey);
    Task SetMemberRoleAsync(string teamKey, string userKey, AccessLevel accessLevel);
    Task SetMemberTenantRolesAsync(string teamKey, string userKey, string[] tenantRoles);
    Task SetMemberScopeOverridesAsync(string teamKey, string userKey, string[] scopeOverrides);
    Task SetMemberNameAsync(string teamKey, string userKey, string name);
    Task<ITeam> SetInvitationResponseAsync(string teamKey, string userKey, string inviteKey, bool accept);
    Task SetConsentAsync(string teamKey, string[] consentedRoles, AccessLevel? accessLevel = null);
    Task SetCustomRolesAsync(string teamKey, IReadOnlyList<TenantRoleDefinition> customRoles);
    IAsyncEnumerable<TTeamEntity> GetTeamsByConsentAsync(string[] roles);

    /// <summary>
    /// Every team, regardless of membership — backs the cross-team discovery path authorized by
    /// <see cref="SystemTeamScopes.Read"/>.
    /// </summary>
    /// <remarks>
    /// Declared with a default implementation so existing custom repositories keep compiling. The default
    /// throws rather than returning empty: a silently empty cross-team list is indistinguishable from a
    /// working feature that happens to have nothing to show, which hides the missing implementation.
    /// </remarks>
    IAsyncEnumerable<TTeamEntity> GetAllTeamsAsync()
        => throw new NotSupportedException(
            $"'{GetType().Name}' does not implement {nameof(GetAllTeamsAsync)}. Implement it to support " +
            $"cross-team listing (the '{SystemTeamScopes.Read}' system scope).");

    /// <summary>
    /// Removes the user's member entries from every team they appear in, regardless of membership state.
    /// Backs user deletion. Returns the number of teams the user was removed from.
    /// </summary>
    /// <remarks>
    /// Declared with a default implementation so existing custom repositories keep compiling. The default
    /// throws rather than returning 0: a silent no-op on a deletion path would leave memberships behind
    /// while reporting success.
    /// </remarks>
    Task<int> RemoveMemberFromAllTeamsAsync(string userKey)
        => throw new NotSupportedException(
            $"'{GetType().Name}' does not implement {nameof(RemoveMemberFromAllTeamsAsync)}. Implement it " +
            $"to support user deletion (the '{SystemUserScopes.Manage}' system scope).");
}