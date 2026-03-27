using System.Collections.Concurrent;
using Tharga.Toolkit;

namespace Tharga.Team;

public abstract class TeamServiceBase : ITeamService
{
    private readonly IUserService _userService;
    private static readonly ConcurrentDictionary<string, ITeamMember> _teamMemberCache = new();

    protected TeamServiceBase(IUserService userService)
    {
        _userService = userService;
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

    public async IAsyncEnumerable<ITeam> GetTeamsAsync()
    {
        var user = await GetCurrentUserAsync();

        await foreach (var team in GetTeamsAsync(user))
        {
            yield return team;
        }
    }

    public async IAsyncEnumerable<ITeam<TMember>> GetTeamsAsync<TMember>() where TMember : ITeamMember
    {
        var user = await GetCurrentUserAsync();

        await foreach (var team in GetTeamsAsync(user))
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
        var user = await GetCurrentUserAsync();

        var displayName = ResolveDisplayName(user);
        name ??= $"{displayName}'s team";

        var teamKey = await GetRandomUnsusedTeamKey();

        var team = await CreateTeamAsync(teamKey, name, user, displayName);

        TeamsListChangedEvent?.Invoke(this, new TeamsListChangedEventArgs());
        SelectTeamEvent?.Invoke(this, new SelectTeamEventArgs(team));

        return team;
    }

    public async Task RenameTeamAsync<TMember>(string teamKey, string name) where TMember : ITeamMember
    {
        await AssureAccessLevel<TMember>(teamKey, AccessLevel.Administrator);

        await SetTeamNameAsync(teamKey, name);

        TeamsListChangedEvent?.Invoke(this, new TeamsListChangedEventArgs());
    }

    public async Task DeleteTeamAsync<TMember>(string teamKey) where TMember : ITeamMember
    {
        await AssureAccessLevel<TMember>(teamKey, AccessLevel.Administrator);

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

    public async Task AddMemberAsync(string teamKey, InviteUserModel model)
    {
        await AddTeamMemberAsync(teamKey, model);
    }

    public async Task RemoveMemberAsync(string teamKey, string userKey)
    {
        await RemoveTeamMemberAsync(teamKey, userKey);
        _teamMemberCache.TryRemove($"{teamKey}.{userKey}", out _);
        TeamsListChangedEvent?.Invoke(this, new TeamsListChangedEventArgs());
    }

    public async Task SetMemberRoleAsync(string teamKey, string userKey, AccessLevel accessLevel)
    {
        await SetTeamMemberRoleAsync(teamKey, userKey, accessLevel);
        _teamMemberCache.TryRemove($"{teamKey}.{userKey}", out _);
    }

    public async Task SetMemberTenantRolesAsync(string teamKey, string userKey, string[] tenantRoles)
    {
        await SetTeamMemberTenantRolesAsync(teamKey, userKey, tenantRoles);
        _teamMemberCache.TryRemove($"{teamKey}.{userKey}", out _);
    }

    public async Task SetMemberScopeOverridesAsync(string teamKey, string userKey, string[] scopeOverrides)
    {
        await SetTeamMemberScopeOverridesAsync(teamKey, userKey, scopeOverrides);
        _teamMemberCache.TryRemove($"{teamKey}.{userKey}", out _);
    }

    public async Task SetInvitationResponseAsync(string teamKey, string userKey, string inviteKey, bool accept)
    {
        if (accept)
        {
            var team = await SetTeamMemberInvitationResponseAsync(teamKey, userKey, inviteKey, true);
            TeamsListChangedEvent?.Invoke(this, new TeamsListChangedEventArgs());
            SelectTeamEvent?.Invoke(this, new SelectTeamEventArgs(team));
        }
        else
        {
            await SetTeamMemberInvitationResponseAsync(teamKey, userKey, inviteKey, false);
        }

        _teamMemberCache.TryRemove($"{teamKey}.{userKey}", out _);
    }

    public async Task SetMemberLastSeenAsync(string teamKey)
    {
        var user = await GetCurrentUserAsync();
        await SetTeamMemberLastSeenAsync(teamKey, user.Key);
        _teamMemberCache.TryRemove($"{teamKey}.{user.Key}", out _);
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

    private async Task AssureAccessLevel<TMember>(string teamKey, AccessLevel accessLevel) where TMember : ITeamMember
    {
        var user = await GetCurrentUserAsync();
        var team = await GetTeamAsync<TMember>(teamKey);
        var member = team.Members.Single(x => x.Key == user.Key);
        if (member.State != MembershipState.Member) throw new InvalidOperationException("User is not a member.");
        if (member.AccessLevel > accessLevel) throw new InvalidOperationException($"Cannot be executed by user {user.EMail} with {member.AccessLevel}.");
    }

    private async Task<IUser> GetCurrentUserAsync()
    {
        var user = await _userService.GetCurrentUserAsync();
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
}