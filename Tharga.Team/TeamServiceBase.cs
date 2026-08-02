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

    /// <summary>Persists a member's suspended state. Override to support suspending members.</summary>
    /// <remarks>
    /// Virtual with a throwing body rather than abstract: adding an abstract member here would break
    /// every host that already derives from this class. Throwing rather than no-opping for the same
    /// reason the user and key equivalents do — a suspension silently skipped is a containment reported
    /// but never applied.
    /// </remarks>
    protected virtual Task SetTeamMemberSuspendedAsync(string teamKey, string userKey, DateTime? suspendedAt, string suspendedBy)
        => throw new NotSupportedException(
            $"'{GetType().Name}' does not implement {nameof(SetTeamMemberSuspendedAsync)}. Implement it, " +
            $"and declare {nameof(ITeamMember.SuspendedAt)}/{nameof(ITeamMember.SuspendedBy)} on your " +
            $"member entity, to support suspending members.");
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

    public Task<ITeam> GetTeamByKeyAsync(string teamKey) => GetTeamAsync(teamKey);

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

    /// <summary>
    /// Sets a member's access level. Ownership is not settable here — it changes only through
    /// <see cref="TransferOwnershipAsync{TMember}"/>, which checks that the caller is the current owner.
    /// </summary>
    /// <remarks>
    /// Both directions are refused. Granting Owner would let any holder of the member-manage scope promote
    /// themselves past that check; demoting the sitting owner would leave a team nobody can transfer, because
    /// transfer requires the caller to be the owner. Transfer itself is unaffected — it calls the protected
    /// <see cref="SetTeamMemberRoleAsync"/> directly.
    /// </remarks>
    public async Task SetMemberRoleAsync(string teamKey, string userKey, AccessLevel accessLevel)
    {
        if (accessLevel == AccessLevel.Owner)
            throw new InvalidOperationException("A member cannot be made owner directly. Transfer ownership instead.");

        var current = await GetTeamMemberAsync(teamKey, userKey);
        if (current?.AccessLevel == AccessLevel.Owner)
            throw new InvalidOperationException("The owner's access level cannot be changed. Transfer ownership first.");

        await SetTeamMemberRoleAsync(teamKey, userKey, accessLevel);
        _teamMemberCache.TryRemove($"{teamKey}.{userKey}", out _);
        TeamsListChangedEvent?.Invoke(this, new TeamsListChangedEventArgs());
    }

    /// <remarks>
    /// Two refusals, both mirroring guards this class already applies elsewhere. <b>The Owner cannot be
    /// suspended</b> — the same reason the owner cannot leave and cannot be demoted: it would leave a team
    /// whose ownership nobody can transfer, since transfer requires the caller to be the owner. <b>A
    /// member cannot suspend themselves</b>, so an administrator who does it needs a second one to undo
    /// it, and somebody is always left holding <c>member:manage</c>.
    /// <para>
    /// The member cache is dropped on both directions, or the claims builder keeps reading the old state
    /// and the suspension takes effect only after the entry ages out.
    /// </para>
    /// </remarks>
    public async Task SetMemberSuspendedAsync(string teamKey, string userKey, bool suspended)
    {
        // The whole roster, not GetTeamMemberAsync. That path resolves through the store's
        // "teams I am a member of" query, which filters on State == Member -- so an invited person comes
        // back null and would be reported as not being in the team at all, which is both wrong and
        // unhelpful. Reading the team directly is the only way to tell the two apart.
        var member = await GetMembersAsync(teamKey).FirstOrDefaultAsync(x => x.Key == userKey);
        if (member == null)
            throw new InvalidOperationException($"User '{userKey}' is not a member of team '{teamKey}'.");

        if (member.State != null && member.State != MembershipState.Member)
        {
            throw new InvalidOperationException(
                $"'{userKey}' has not accepted the invitation to team '{teamKey}', so there is no access " +
                $"to suspend. Withdraw the invitation instead.");
        }

        if (suspended)
        {
            if (member.AccessLevel == AccessLevel.Owner)
                throw new InvalidOperationException("The owner cannot be suspended. Transfer ownership first.");

            var caller = await GetCurrentUserAsync();
            if (caller != null && string.Equals(caller.Key, userKey, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("You cannot suspend your own membership. Ask another administrator to do it.");
        }

        var actor = suspended ? (await GetCurrentUserAsync())?.Key : null;
        await SetTeamMemberSuspendedAsync(teamKey, userKey, suspended ? DateTime.UtcNow : null, actor);

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
    /// <summary>
    /// Backs <see cref="GetTeamsForUserWithAccessLevelAsync"/>. Virtual-throw rather than returning an
    /// empty list, because an empty list is indistinguishable from "this user owns nothing" — and the
    /// caller uses that answer to decide whether deleting them is safe. A silent empty default would
    /// suppress exactly the warning this exists to raise.
    /// </summary>
    protected virtual Task<IReadOnlyList<ITeam>> GetTeamsForUserWithAccessLevelInternalAsync(string userKey, AccessLevel accessLevel)
        => throw new NotSupportedException(
            $"'{GetType().Name}' does not implement {nameof(GetTeamsForUserWithAccessLevelInternalAsync)}. " +
            "Implement it so user deletion can warn about teams the user owns.");

    public Task<IReadOnlyList<ITeam>> GetTeamsForUserWithAccessLevelAsync(string userKey, AccessLevel accessLevel)
        => GetTeamsForUserWithAccessLevelInternalAsync(userKey, accessLevel);

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
            "No IIconStore was supplied to this service. Team icons require one, and there are two ways to " +
            "be missing it: (a) none is registered — the built-in MongoIconStore comes from " +
            "AddThargaTeamRepository, or supply your own via o.AddIconStore<T>(); or (b) it IS registered " +
            "but this service did not receive it — TeamServiceRepositoryBase takes an optional " +
            "'IIconStore iconStore = null' constructor parameter, so a subclass that does not forward it " +
            "gets null here. See docs/articles/icons.md.");

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

    public async Task AssignOwnerAsync<TMember>(string teamKey, string newOwnerUserKey) where TMember : ITeamMember
    {
        var team = await GetTeamAsync<TMember>(teamKey)
            ?? throw new InvalidOperationException($"Team '{teamKey}' was not found.");

        var members = team.Members?.Cast<ITeamMember>().ToArray() ?? [];

        // Refusing loudly on a healthy team matters more than the happy path: this is the one operation
        // that can hand out Owner without a sitting owner's consent.
        if (!TeamOwnership.IsOwnerless(members))
            throw new InvalidOperationException(
                $"Team '{teamKey}' already has an owner. Assigning an owner repairs an ownerless team; " +
                $"use {nameof(TransferOwnershipAsync)} to move ownership within a team that has one.");

        if (!TeamOwnership.CanAssign(members, newOwnerUserKey))
            throw new InvalidOperationException(
                $"User '{newOwnerUserKey}' is not a member of team '{teamKey}'. An owner is chosen from " +
                "the team's existing members, so repairing a team cannot introduce someone new to it.");

        await SetTeamMemberRoleAsync(teamKey, newOwnerUserKey, AccessLevel.Owner);
        _teamMemberCache.TryRemove($"{teamKey}.{newOwnerUserKey}", out _);
        TeamsListChangedEvent?.Invoke(this, new TeamsListChangedEventArgs());
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