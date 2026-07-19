using System.Security.Claims;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using Tharga.Team.Blazor.Framework;
using Tharga.Team;

namespace Tharga.Team.Blazor.Features.Team;

internal class TeamStateService : ITeamStateService
{
    private readonly ITeamService _teamService;
    private readonly NavigationManager _navigationManager;
    private readonly ILocalStorageService _localStorageService;
    private readonly IJSRuntime _jSRuntime;
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private readonly ThargaBlazorOptions _options;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private ITeam _selectedTeam;

    public TeamStateService(ITeamService teamService, NavigationManager navigationManager, ILocalStorageService localStorageService, IJSRuntime jSRuntime, AuthenticationStateProvider authenticationStateProvider, IOptions<ThargaBlazorOptions> options)
    {
        _teamService = teamService;
        _navigationManager = navigationManager;
        _localStorageService = localStorageService;
        _jSRuntime = jSRuntime;
        _authenticationStateProvider = authenticationStateProvider;
        _options = options.Value;

        _teamService.TeamsListChangedEvent += (s, e) => { TeamsListChangedEvent?.Invoke(s, e); };
    }

    public event EventHandler<TeamsListChangedEventArgs> TeamsListChangedEvent;
    public event EventHandler<SelectedTeamChangedEventArgs> SelectedTeamChangedEvent;

    public async Task<ITeam> GetSelectedTeamAsync()
    {
        var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
        if (!(authState.User.Identity?.IsAuthenticated ?? false)) return null;

        try
        {
            await _semaphore.WaitAsync();

            // Two sets, two purposes. `teams` (own memberships) drives every *automatic* choice below;
            // `visibleTeams` only decides whether an existing selection is still legitimate. Defaulting
            // from the widened set would silently park an oversight caller inside an arbitrary tenant.
            var teams = await _teamService.GetTeamsAsync().ToArrayAsync();
            var visibleTeams = await GetVisibleTeamsAsync(authState.User, teams);

            if (_selectedTeam == null || visibleTeams.All(x => x.Key != _selectedTeam.Key) || visibleTeams.FirstOrDefault(x => x.Key == _selectedTeam.Key)?.Name != _selectedTeam.Name)
            {
                var t = authState.User.Claims.FirstOrDefault(x => x.Type == Constants.TeamKeyCookie);
                if (t != null)
                {
                    var team = visibleTeams.FirstOrDefault(x => x.Key == t.Value) ?? teams.FirstOrDefault();
                    await AssignTeamAsync(team);
                }
                else if (!teams.Any())
                {
                    if (_options.AutoCreateFirstTeam)
                    {
                        var team = await _teamService.CreateTeamAsync();
                        await AssignTeamAsync(team, true);
                    }
                }
                else if (teams.Length == 1)
                {
                    await AssignTeamAsync(teams.Single(), true);
                }
                else
                {
                    var teamKey = await _localStorageService.GetItemAsStringAsync(Constants.SelectedTeamLocalStorageKey);
                    await AssignTeamAsync(teams.FirstOrDefault(x => x.Key == teamKey) ?? teams.FirstOrDefault(), true);
                }
            }

            return _selectedTeam;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Teams the caller may legitimately have selected: their own, widened to every team when they hold
    /// <see cref="SystemTeamScopes.Read"/>. Falls back to own teams if the widened read is refused, so a
    /// claims/enforcement mismatch degrades to today's behaviour instead of breaking the page.
    /// </summary>
    private async Task<ITeam[]> GetVisibleTeamsAsync(ClaimsPrincipal principal, ITeam[] ownTeams)
    {
        if (!TeamVisibility.CanSeeAllTeams(principal)) return ownTeams;

        try
        {
            return await _teamService.GetAllTeamsAsync().ToArrayAsync();
        }
        catch (UnauthorizedAccessException)
        {
            return ownTeams;
        }
    }

    private async Task AssignTeamAsync(ITeam team, bool refresh = false)
    {
        _selectedTeam = team;

        if (refresh && team != null)
        {
            await _jSRuntime.InvokeVoidAsync("eval", $"document.cookie = 'selected_team_id={team.Key}; path=/'");
            _navigationManager.Refresh(true);
            return;
        }

        SelectedTeamChangedEvent?.Invoke(this, new SelectedTeamChangedEventArgs(_selectedTeam));
    }

    public async Task SetSelectedTeamAsync(ITeam selectedTeam)
    {
        await _teamService.SetMemberLastSeenAsync(selectedTeam.Key);

        if (_selectedTeam?.Key == selectedTeam.Key) return;

        _selectedTeam = selectedTeam;

        // Only remember a team the caller actually belongs to. An oversight caller viewing someone else's
        // team gets a session-scoped selection (cookie only) rather than being parked there indefinitely.
        var ownTeams = await _teamService.GetTeamsAsync().ToArrayAsync();
        if (ownTeams.Any(x => x.Key == selectedTeam.Key))
        {
            await _localStorageService.SetItemAsStringAsync(Constants.SelectedTeamLocalStorageKey, selectedTeam.Key);
        }

        await _jSRuntime.InvokeVoidAsync("eval", $"document.cookie = '{Constants.SelectedTeamKeyCookie}={_selectedTeam?.Key}; path=/'");
        _navigationManager.Refresh(true);
    }
}