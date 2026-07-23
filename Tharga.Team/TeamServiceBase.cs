using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Tharga.Toolkit;

namespace Tharga.Team;

public abstract class TeamServiceBase : ITeamService
{
    private readonly IUserService _userService;
    private readonly ILogger<TeamServiceBase> _logger;
    private readonly IIconStore _iconStore;
    private static readonly ConcurrentDictionary<string, ITeamMember> _teamMemberCache = new();

    protected TeamServiceBase(IUserService userService, ILogger<TeamServiceBase> logger = null, IIconStore iconStore = null)
    {
        _userService = userService;
        _logger = logger;
        _iconStore = iconStore;
    }

    public event EventHandler<TeamsListChangedEventArgs> TeamsListChangedEvent;
    public event EventHandler<SelectTeamEventArgs> SelectTeamEvent;

    protected abstract IAsyncEnumerable<ITeam> GetTeamsAsync(IUser user);
    protected abstract Task<ITeam> GetTeamAsync(string teamKey);
    protected abstract Task<ITeam> CreateTeamAsync(string teamKey, string name, IUser user, string displayName = null);
    protected abstract Task SetTeamNameAsync(string teamKey, string name);
    protected abstract Task DeleteTeamAsync(string teamKey);
    protected abstract Task AddTeamMemberAsync(string teamKey, InviteUserModel model);
    protected abstract Task RemoveTeamMemberAsync(string teamKey, string userKey);
    protected abstract Task<ITeam> SetTeamMemberInvitationResponseAsync(string teamKey, string userKey, string inviteKey, bool accept);
    protected abstract Task SetTeamMemberLastSeenAsync(string teamKey, string userKey);
    protected abstract Task<ITeamMember> GetTeamMembersAsync(string teamKey, string userKey);
    protected abstract Task SetTeamMemberRoleAsync(string teamKey, string userKey, AccessLevel accessLevel);
    protected abstract Task SetTeamMemberTenantRolesAsync(string teamKey, string userKey, string[] tenantRoles);
    protected abstract Task SetTeamMemberScopeOverridesAsync(string teamKey, string userKey, string[] scopeOverrides);
    protected abstract Task SetTeamMemberNameAsync(string teamKey, string userKey, string name);
    protected abstract Task SetTeamConsentInternalAsync(string teamKey, string[] consentedRoles, AccessLevel? accessLevel);
    protected abstract IAsyncEnumerable<ITeam> GetConsentedTeamsInternalAsync(string[] userRoles);
    protected abstract Task SetTeamCustomRolesInternalAsync(string teamKey, IReadOnlyList<TenantRoleDefinition> customRoles);

    public async IAsyncEnumerable<ITeam> GetTeamsAsync()
    {
        var user = await GetCurrentUserAsync();
        if (user == null) yield break;

        await foreach (var team in GetTeamsAsync(user))
        {
            yield return team;
        }
    }

    public async IAsyncEnumerable<ITeam<TMember>> GetTeamsAsync<TMember>() where TMember : ITeamMember
    {
        var user = await GetCurrentUserAsync();
        if (user == null) yield break;

        await foreach (var team in GetTeamsAsync(user))
        {
            yield return (ITeam<TMember>)team;
        }
    }

    /// <summary>
    /// Backs <see cref="GetAllTeamsAsync()"/>. Virtual rather than abstract so existing derived services
    /// keep compiling; the default returns nothing, and storage-backed bases override it.
    /// </summary>
    protected virtual async IAsyncEnumerable<ITeam> GetAllTeamsInternalAsync()
    {
        await Task.CompletedTask;
        yield break;
    }

    public virtual IAsyncEnumerable<ITeam> GetAllTeamsAsync() => GetAllTeamsInternalAsync();

    public virtual async IAsyncEnumerable<ITeam<TMember>> GetAllTeamsAsync<TMember>() where TMember : ITeamMember
    {
        await foreach (var team in GetAllTeamsInternalAsync())
        {
            yield return (ITeam<TMember>)team;
        }
    }

    public async Task<ITeam<TMember>> GetTeamAsync<TMember>(string teamKey) where TMember : ITeamMember
    {
        var team = await GetTeamAsync(teamKey);
        return (ITeam<TMember>)team;
    }

    public async Task<ITeam> CreateTeamAsync(string name)
    {
        var user = await RequireCurrentUserAsync();

        var displayName = ResolveDisplayName(user);
        name ??= $"{displayName}'s team";

        var teamKey = await GetRandomUnsusedTeamKey();

        var team = await CreateTeamAsync(teamKey, name, user, displayName);

        TeamsListChangedEvent?.Invoke(this, new TeamsListChangedEventArgs());
        SelectTeamEvent?.Invoke(this, new SelectTeamEventArgs(team));

        return team;
    }

    // Authorization (team:manage / teams:delete) is enforced by AuthorizationTeamServiceDecorator at the
    // service boundary, so it applies uniformly to admin users and team API keys. These methods perform the
    // operation; they assume the caller is already authorized.
    public async Task RenameTeamAsync<TMember>(string teamKey, string name) where TMember : ITeamMember
    {
        await SetTeamNameAsync(teamKey, name);

        TeamsListChangedEvent?.Invoke(this, new TeamsListChangedEventArgs());
    }

    public async Task DeleteTeamAsync<TMember>(string teamKey) where TMember : ITeamMember
    {
        await DeleteTeamAsync(teamKey);

        TeamsListChangedEvent?.Invoke(this, new TeamsListChangedEventArgs());
    }

    public async Task<ITeamMember> GetTeamMemberAsync(string teamKey, string userKey)
    {
        var key = $"{teamKey}.{userKey}";
        if (_teamMemberCache.TryGetValue(key, out var teamMember)) return teamMember;

        teamMember = await GetTeamMembersAsync(teamKey, userKey);

        _teamMemberCache.TryAdd(key, teamMember);

        return teamMember;
    }

    public virtual async IAsyncEnumerable<ITeamMember> GetMembersAsync(string teamKey)
    {
        var team = await GetTeamAsync(teamKey);
        var members = GetMembersFromTeam(team);
        if (members == null) yield break;
        foreach (var member in members)
        {
            yield return member;
        }
    }

    public async Task AddMemberAsync(string teamKey, InviteUserModel model)
    {
        await AddTeamMemberAsync(teamKey, model);
        TeamsListChangedEvent?.Invoke(this, new TeamsListChangedEventArgs());
    }

    public async Task RemoveMemberAsync(string teamKey, string userKey)
    {
        var team = await GetTeamAsync(teamKey);
        var members = GetMembersFromTeam(team);
        if (members != null)
        {
            var member = members.PickOneOrDefault(x => x.Key == userKey, _logger, teamKey, userKey);
            if (member != null)
            {
                if (member.AccessLevel == AccessLevel.Owner)
                    throw new InvalidOperationException("The owner cannot leave the team. Transfer ownership first.");

                var user = await RequireCurrentUserAsync();
                if (member.Key == user.Key && member.AccessLevel == AccessLevel.Administrator)
                {
                    var otherAdminsOrOwners = members.Count(x =>
                        x.Key != userKey &&
                        x.State == MembershipState.Member &&
                        x.AccessLevel <= AccessLevel.Administrator);
                    if (otherAdminsOrOwners == 0)
                        throw new InvalidOperationException("Cannot leave the team as the last administrator.");
                }
            }
        }

        await RemoveTeamMemberAsync(teamKey, userKey);
        _teamMemberCache.TryRemove($"{teamKey}.{userKey}", out _);
        TeamsListChangedEvent?.Invoke(this, new TeamsListChangedEventArgs());
    }

    public async Task SetMemberRoleAsync(string teamKey, string userKey, AccessLevel accessLevel)
    {
        await SetTeamMemberRoleAsync(teamKey, userKey, accessLevel);
        _teamMemberCache.TryRemove($"{teamKey}.{userKey}", out _);
        TeamsListChangedEvent?.Invoke(this, new TeamsListChangedEventArgs());
    }

    public async Task SetMemberTenantRolesAsync(string teamKey, string userKey, string[] tenantRoles)
    {
        await SetTeamMemberTenantRolesAsync(teamKey, userKey, tenantRoles);
        _teamMemberCache.TryRemove($"{teamKey}.{userKey}", out _);
        TeamsListChangedEvent?.Invoke(this, new TeamsListChangedEventArgs());
    }

    public async Task SetMemberScopeOverridesAsync(string teamKey, string userKey, string[] scopeOverrides)
    {
        await SetTeamMemberScopeOverridesAsync(teamKey, userKey, scopeOverrides);
        _teamMemberCache.TryRemove($"{teamKey}.{userKey}", out _);
        TeamsListChangedEvent?.Invoke(this, new TeamsListChangedEventArgs());
    }

    public async Task SetMemberNameAsync(string teamKey, string userKey, string name)
    {
        await SetTeamMemberNameAsync(teamKey, userKey, name);
        _teamMemberCache.TryRemove($"{teamKey}.{userKey}", out _);
        TeamsListChangedEvent?.Invoke(this, new TeamsListChangedEventArgs());
    }

    public async Task SetInvitationResponseAsync(string teamKey, string userKey, string inviteKey, bool accept)
    {
        if (accept)
        {
            // Capture the admin-entered Member.Name *before* the accept clears it, so we can
            // promote it to User.Name (only-if-empty) once the response has been recorded.
            var seedName = await GetInvitedMemberNameAsync(teamKey, inviteKey);

            var team = await SetTeamMemberInvitationResponseAsync(teamKey, userKey, inviteKey, true);

            if (!string.IsNullOrWhiteSpace(seedName))
            {
                await _userService.SeedUserNameAsync(userKey, seedName);
            }

            TeamsListChangedEvent?.Invoke(this, new TeamsListChangedEventArgs());
            SelectTeamEvent?.Invoke(this, new SelectTeamEventArgs(team));
        }
        else
        {
            await SetTeamMemberInvitationResponseAsync(teamKey, userKey, inviteKey, false);
            TeamsListChangedEvent?.Invoke(this, new TeamsListChangedEventArgs());
        }

        _teamMemberCache.TryRemove($"{teamKey}.{userKey}", out _);
    }

    /// <summary>
    /// Look up the (admin-entered) Name of the member identified by <paramref name="inviteKey"/>
    /// inside the given team. Used to capture the invitation Name *before* accept clears it,
    /// so it can be promoted to <c>User.Name</c>. Default implementation returns null;
    /// derivatives that have access to the typed team document override it.
    /// </summary>
    protected virtual Task<string> GetInvitedMemberNameAsync(string teamKey, string inviteKey)
    {
        return Task.FromResult<string>(null);
    }

    public async Task SetMemberLastSeenAsync(string teamKey)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return;
        await SetTeamMemberLastSeenAsync(teamKey, user.Key);
        _teamMemberCache.TryRemove($"{teamKey}.{user.Key}", out _);
    }

    /// <summary>
    /// Backs <see cref="RemoveUserFromAllTeamsAsync"/>. Virtual rather than abstract so existing derived
    /// services keep compiling; the default throws rather than returning 0, since a silent no-op on a
    /// deletion path would hide the missing implementation. Storage-backed bases override it.
    /// </summary>
    protected virtual Task<int> RemoveUserFromAllTeamsInternalAsync(string userKey)
        => throw new NotSupportedException(
            $"'{GetType().Name}' does not implement {nameof(RemoveUserFromAllTeamsInternalAsync)}. " +
            $"Implement it to support user deletion (the '{SystemUserScopes.Manage}' system scope).");

    /// <summary>
    /// Backs <see cref="SetTeamIconAsync"/> / <see cref="ClearTeamIconAsync"/> — persists the icon
    /// reference (or null to clear) on the team document. Virtual-throw so existing derived services keep
    /// compiling; storage-backed bases override it.
    /// </summary>
    protected virtual Task SetTeamIconReferenceInternalAsync(string teamKey, string reference)
        => throw new NotSupportedException(
            $"'{GetType().Name}' does not implement {nameof(SetTeamIconReferenceInternalAsync)}. Implement it to support team icons.");

    public async Task SetTeamIconAsync(string teamKey, byte[] data, string contentType)
    {
        var store = RequireIconStore();

        var team = await GetTeamAsync(teamKey);
        var previousReference = team?.Icon;

        var reference = await store.SaveAsync(IconKind.Team, teamKey, data, contentType);
        await SetTeamIconReferenceInternalAsync(teamKey, reference);

        if (!string.IsNullOrEmpty(previousReference))
            await store.DeleteAsync(previousReference);

        TeamsListChangedEvent?.Invoke(this, new TeamsListChangedEventArgs());
    }

    public async Task ClearTeamIconAsync(string teamKey)
    {
        var store = RequireIconStore();

        var team = await GetTeamAsync(teamKey);
        var previousReference = team?.Icon;
        if (string.IsNullOrEmpty(previousReference)) return;

        await SetTeamIconReferenceInternalAsync(teamKey, null);
        await store.DeleteAsync(previousReference);

        TeamsListChangedEvent?.Invoke(this, new TeamsListChangedEventArgs());
    }

    private IIconStore RequireIconStore()
        => _iconStore ?? throw new NotSupportedException(
            "No IIconStore is registered. Team icons require a registered icon store (the built-in " +
            "MongoIconStore is registered by AddThargaTeamRepository, or supply one via o.AddIconStore<T>()).");

    public async Task<int> RemoveUserFromAllTeamsAsync(string userKey)
    {
        var count = await RemoveUserFromAllTeamsInternalAsync(userKey);

        foreach (var cacheKey in _teamMemberCache.Keys.Where(x => x.EndsWith($".{userKey}")).ToArray())
        {
            _teamMemberCache.TryRemove(cacheKey, out _);
        }

        if (count > 0)
        {
            TeamsListChangedEvent?.Invoke(this, new TeamsListChangedEventArgs());
        }

        return count;
    }

    public async Task TransferOwnershipAsync<TMember>(string teamKey, string newOwnerUserKey) where TMember : ITeamMember
    {
        var user = await RequireCurrentUserAsync();
        var team = await GetTeamAsync<TMember>(teamKey);
        var currentOwner = team.Members.PickOneOrDefault(x => x.Key == user.Key, _logger, teamKey, user.Key);
        if (currentOwner == null || currentOwner.AccessLevel != AccessLevel.Owner)
            throw new InvalidOperationException("Only the current owner can transfer ownership.");

        var newOwner = team.Members.PickOneOrDefault(x => x.Key == newOwnerUserKey, _logger, teamKey, newOwnerUserKey);
        if (newOwner == null)
            throw new InvalidOperationException($"User '{newOwnerUserKey}' is not a member of this team.");
        if (newOwner.Key == user.Key)
            throw new InvalidOperationException("Cannot transfer ownership to yourself.");

        await SetTeamMemberRoleAsync(teamKey, newOwnerUserKey, AccessLevel.Owner);
        await SetTeamMemberRoleAsync(teamKey, user.Key, AccessLevel.Administrator);
        _teamMemberCache.TryRemove($"{teamKey}.{newOwnerUserKey}", out _);
        _teamMemberCache.TryRemove($"{teamKey}.{user.Key}", out _);
        TeamsListChangedEvent?.Invoke(this, new TeamsListChangedEventArgs());
    }

    public async Task SetTeamConsentAsync(string teamKey, string[] consentedRoles, AccessLevel? accessLevel = null)
    {
        await SetTeamConsentInternalAsync(teamKey, consentedRoles, accessLevel);
        TeamsListChangedEvent?.Invoke(this, new TeamsListChangedEventArgs());
    }

    public IAsyncEnumerable<ITeam> GetConsentedTeamsAsync(string[] userRoles)
    {
        return GetConsentedTeamsInternalAsync(userRoles);
    }

    public async Task<IReadOnlyList<TenantRoleDefinition>> GetTeamCustomRolesAsync(string teamKey)
    {
        var team = await GetTeamAsync(teamKey);
        return team?.CustomRoles ?? Array.Empty<TenantRoleDefinition>();
    }

    public async Task SetTeamCustomRolesAsync(string teamKey, IReadOnlyList<TenantRoleDefinition> customRoles)
    {
        await SetTeamCustomRolesInternalAsync(teamKey, customRoles);
        TeamsListChangedEvent?.Invoke(this, new TeamsListChangedEventArgs());
    }

    private async Task<string> GetRandomUnsusedTeamKey()
    {
        string teamKey;
        while (true)
        {
            teamKey = StringExtension.UpperCaseAlphaNumericCharacters.Random();
            var item = await GetTeamAsync(teamKey);
            if (item == null) break;
        }

        return teamKey;
    }


    private async Task<IUser> GetCurrentUserAsync()
    {
        var user = await _userService.GetCurrentUserAsync();
        return user;
    }

    private async Task<IUser> RequireCurrentUserAsync()
    {
        var user = await GetCurrentUserAsync();
        if (user == null) throw new UnauthorizedAccessException("Authentication required.");
        return user;
    }

    public static string ResolveDisplayName(IUser user)
    {
        if (user == null) return "Unknown";

        if (!string.IsNullOrEmpty(user.Name))
            return user.Name;

        var email = user.EMail;
        if (string.IsNullOrEmpty(email))
            return "Unknown";

        var atIndex = email.IndexOf('@');
        var username = atIndex >= 0 ? email[..atIndex] : email;
        var words = username.Split('.');
        return string.Join(" ", words.Select(w =>
            w.Length > 0 ? char.ToUpper(w[0]) + w[1..] : w));
    }

    private static ITeamMember[] GetMembersFromTeam(ITeam team)
    {
        var membersProperty = team?.GetType().GetProperty("Members");
        return membersProperty?.GetValue(team) as ITeamMember[];
    }
}