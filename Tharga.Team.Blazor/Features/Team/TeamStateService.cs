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

            // Two sets, two purposes. `visibleTeams` (widened for a teams:read holder) decides which
            // *chosen* team is still legitimate; `teams` (own memberships) is the only source for the
            // fallback, so nobody is ever defaulted into a tenant they didn't pick.
            var teams = await _teamService.GetTeamsAsync().ToArrayAsync();
            var visibleTeams = await GetVisibleTeamsAsync(authState.User, teams);

            if (_selectedTeam == null || visibleTeams.All(x => x.Key != _selectedTeam.Key) || visibleTeams.FirstOrDefault(x => x.Key == _selectedTeam.Key)?.Name != _selectedTeam.Name)
            {
                var currentTeamKey = authState.User.Claims.FirstOrDefault(x => x.Type == Constants.TeamKeyCookie)?.Value;
                var rememberedTeamKey = await _localStorageService.GetItemAsStringAsync(Constants.SelectedTeamLocalStorageKey);
                var team = TeamSelectionResolver.Resolve(currentTeamKey, rememberedTeamKey, visibleTeams, teams);

                if (team == null && !teams.Any() && _options.AutoCreateFirstTeam)
                {
                    team = await _teamService.CreateTeamAsync();
                }

                // Refresh only when the cookie doesn't already name this team — otherwise the claims for
                // it have been applied on this request and a reload would be pointless.
                await AssignTeamAsync(team, team != null && team.Key != currentTeamKey);
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

        // Remembered across visits whether or not the caller is a member — an explicit choice is theirs
        // to keep. Selection carries no access on its own: a non-member gets scopes only where the team
        // has consented to a role they hold.
        await _localStorageService.SetItemAsStringAsync(Constants.SelectedTeamLocalStorageKey, selectedTeam.Key);

        await _jSRuntime.InvokeVoidAsync("eval", $"document.cookie = '{Constants.SelectedTeamKeyCookie}={_selectedTeam?.Key}; path=/'");
        _navigationManager.Refresh(true);
    }
}