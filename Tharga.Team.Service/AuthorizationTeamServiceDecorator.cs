using Tharga.Team;

namespace Tharga.Team.Service;

/// <summary>
/// Decorator over <see cref="ITeamService"/> that enforces the team-operation authorization model in the
/// service layer (so the same checks protect the Blazor circuit and any consumer's REST controller). Reads
/// the caller's claims via <see cref="TeamAuthorizer"/>:
/// <list type="bullet">
/// <item>Create — authenticated AND <c>AllowTeamCreation</c> (no scope; self-service).</item>
/// <item>Delete — (<c>team:manage</c> on the team AND <c>AllowTeamCreation</c>) OR <c>teams:delete</c> (system).</item>
/// <item>Rename / Consent — <c>team:manage</c> on the team.</item>
/// <item>Member invite/remove/role/scope-overrides/display-name — <c>member:manage</c> on the team.</item>
/// <item>Transfer ownership — passed through (Owner-only is enforced by the inner service).</item>
/// </list>
/// Reads, consent-team lookup, last-seen touch, and invitation responses pass through (self-service / not gated here).
/// </summary>
public sealed class AuthorizationTeamServiceDecorator : ITeamService
{
    private readonly ITeamService _inner;
    private readonly TeamAuthorizer _authorizer;
    private readonly TeamLifecycleOptions _lifecycle;
    private readonly IScopeRegistry _scopeRegistry;
    private readonly ITenantRoleRegistry _tenantRoleRegistry;

    public AuthorizationTeamServiceDecorator(ITeamService inner, TeamAuthorizer authorizer, TeamLifecycleOptions lifecycle, IScopeRegistry scopeRegistry = null, ITenantRoleRegistry tenantRoleRegistry = null)
    {
        _inner = inner;
        _authorizer = authorizer;
        _lifecycle = lifecycle;
        _scopeRegistry = scopeRegistry;
        _tenantRoleRegistry = tenantRoleRegistry;
    }

    public event EventHandler<TeamsListChangedEventArgs> TeamsListChangedEvent
    {
        add => _inner.TeamsListChangedEvent += value;
        remove => _inner.TeamsListChangedEvent -= value;
    }

    public event EventHandler<SelectTeamEventArgs> SelectTeamEvent
    {
        add => _inner.SelectTeamEvent += value;
        remove => _inner.SelectTeamEvent -= value;
    }

    // Reads & self-service — pass through.
    public IAsyncEnumerable<ITeam> GetTeamsAsync() => _inner.GetTeamsAsync();
    public IAsyncEnumerable<ITeam<TMember>> GetTeamsAsync<TMember>() where TMember : ITeamMember => _inner.GetTeamsAsync<TMember>();
    public Task<ITeam<TMember>> GetTeamAsync<TMember>(string teamKey) where TMember : ITeamMember => _inner.GetTeamAsync<TMember>(teamKey);
    public Task<ITeamMember> GetTeamMemberAsync(string teamKey, string userKey) => _inner.GetTeamMemberAsync(teamKey, userKey);
    public IAsyncEnumerable<ITeamMember> GetMembersAsync(string teamKey) => _inner.GetMembersAsync(teamKey);
    public IAsyncEnumerable<ITeam> GetConsentedTeamsAsync(string[] userRoles) => _inner.GetConsentedTeamsAsync(userRoles);
    public Task<IReadOnlyList<TenantRoleDefinition>> GetTeamCustomRolesAsync(string teamKey) => _inner.GetTeamCustomRolesAsync(teamKey);
    public Task SetMemberLastSeenAsync(string teamKey) => _inner.SetMemberLastSeenAsync(teamKey);
    public Task SetInvitationResponseAsync(string teamKey, string userKey, string inviteCode, bool accept) => _inner.SetInvitationResponseAsync(teamKey, userKey, inviteCode, accept);
    public Task TransferOwnershipAsync<TMember>(string teamKey, string newOwnerUserKey) where TMember : ITeamMember => _inner.TransferOwnershipAsync<TMember>(teamKey, newOwnerUserKey);

    // Lifecycle.
    public async Task<ITeam> CreateTeamAsync(string name)
    {
        await RequireCreateAsync();
        return await _inner.CreateTeamAsync(name);
    }

    public async Task DeleteTeamAsync<TMember>(string teamKey) where TMember : ITeamMember
    {
        await RequireDeleteAsync(teamKey);
        await _inner.DeleteTeamAsync<TMember>(teamKey);
    }

    // Team administration (team:manage on the team).
    public async Task RenameTeamAsync<TMember>(string teamKey, string name) where TMember : ITeamMember
    {
        await RequireTeamScopeAsync(TeamScopes.Manage, teamKey);
        await _inner.RenameTeamAsync<TMember>(teamKey, name);
    }

    public async Task SetTeamConsentAsync(string teamKey, string[] consentedRoles, AccessLevel? accessLevel = null)
    {
        await RequireTeamScopeAsync(TeamScopes.Manage, teamKey);
        await _inner.SetTeamConsentAsync(teamKey, consentedRoles, accessLevel);
    }

    public async Task SetTeamCustomRolesAsync(string teamKey, IReadOnlyList<TenantRoleDefinition> customRoles)
    {
        await RequireTeamScopeAsync(TeamScopes.Manage, teamKey);
        ValidateCustomRoles(customRoles);
        await _inner.SetTeamCustomRolesAsync(teamKey, customRoles);
    }

    // Member administration (member:manage on the team).
    public async Task AddMemberAsync(string teamKey, InviteUserModel model)
    {
        await RequireTeamScopeAsync(TeamScopes.MemberManage, teamKey);
        await _inner.AddMemberAsync(teamKey, model);
    }

    public async Task RemoveMemberAsync(string teamKey, string userKey)
    {
        await RequireTeamScopeAsync(TeamScopes.MemberManage, teamKey);
        await _inner.RemoveMemberAsync(teamKey, userKey);
    }

    public async Task SetMemberRoleAsync(string teamKey, string userKey, AccessLevel accessLevel)
    {
        await RequireTeamScopeAsync(TeamScopes.MemberManage, teamKey);
        await _inner.SetMemberRoleAsync(teamKey, userKey, accessLevel);
    }

    public async Task SetMemberTenantRolesAsync(string teamKey, string userKey, string[] tenantRoles)
    {
        await RequireTeamScopeAsync(TeamScopes.MemberManage, teamKey);
        await _inner.SetMemberTenantRolesAsync(teamKey, userKey, tenantRoles);
    }

    public async Task SetMemberScopeOverridesAsync(string teamKey, string userKey, string[] scopeOverrides)
    {
        await RequireTeamScopeAsync(TeamScopes.MemberManage, teamKey);
        await _inner.SetMemberScopeOverridesAsync(teamKey, userKey, scopeOverrides);
    }

    public async Task SetMemberNameAsync(string teamKey, string userKey, string name)
    {
        await RequireTeamScopeAsync(TeamScopes.MemberManage, teamKey);
        await _inner.SetMemberNameAsync(teamKey, userKey, name);
    }

    private async Task RequireCreateAsync()
    {
        if (!_lifecycle.AllowTeamCreation)
            throw new UnauthorizedAccessException("Team creation is disabled (AllowTeamCreation = false).");
        if (!await _authorizer.IsAuthenticatedAsync())
            throw new UnauthorizedAccessException("Authentication is required to create a team.");
    }

    private async Task RequireDeleteAsync(string teamKey)
    {
        if (await _authorizer.HasSystemScopeAsync(SystemTeamScopes.Delete)) return;
        if (_lifecycle.AllowTeamCreation && await _authorizer.HasTeamScopeAsync(TeamScopes.Manage, teamKey)) return;
        throw new UnauthorizedAccessException(
            $"Deleting team '{teamKey}' requires '{TeamScopes.Manage}' on that team with AllowTeamCreation enabled, " +
            $"or the '{SystemTeamScopes.Delete}' system scope.");
    }

    private async Task RequireTeamScopeAsync(string scope, string teamKey)
    {
        if (!await _authorizer.HasTeamScopeAsync(scope, teamKey))
            throw new UnauthorizedAccessException($"This operation on team '{teamKey}' requires the '{scope}' scope on that team.");
    }

    /// <summary>
    /// Guards against privilege escalation and ambiguity when defining custom roles: every scope must be
    /// app-registered (<see cref="IScopeRegistry"/>), names must be non-empty and unique, and must not
    /// collide with a code-registered role name.
    /// </summary>
    private void ValidateCustomRoles(IReadOnlyList<TenantRoleDefinition> customRoles)
    {
        if (customRoles == null) return;

        var registeredScopes = _scopeRegistry?.All.Select(s => s.Name).ToHashSet(StringComparer.Ordinal);
        var codeRoleNames = _tenantRoleRegistry?.All.Select(r => r.Name).ToHashSet(StringComparer.Ordinal)
                            ?? [];
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var role in customRoles)
        {
            if (string.IsNullOrWhiteSpace(role.Name))
                throw new InvalidOperationException("A custom role name must not be empty.");

            var name = role.Name.Trim();

            if (!seen.Add(name))
                throw new InvalidOperationException($"Duplicate custom role name '{name}'.");

            if (codeRoleNames.Contains(name))
                throw new InvalidOperationException($"Custom role '{name}' collides with a code-registered role of the same name.");

            foreach (var scope in role.Scopes ?? [])
            {
                if (registeredScopes == null || !registeredScopes.Contains(scope))
                    throw new InvalidOperationException(
                        $"Custom role '{name}' references scope '{scope}', which is not an app-registered scope.");
            }
        }
    }
}
