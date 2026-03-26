using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace Tharga.Team.Blazor.Framework;

/// <summary>
/// Server-side claims transformation that reads the selected_team_id cookie
/// and enriches the principal with team, role, access level, and scope claims.
/// Registered automatically by AddThargaTeamBlazor — works for Server, SSR, and hybrid apps.
/// </summary>
internal class TeamServerClaimsTransformation : IClaimsTransformation
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ITeamService _teamService;
    private readonly IUserService _userService;
    private readonly IScopeRegistry _scopeRegistry;

    public TeamServerClaimsTransformation(
        IHttpContextAccessor httpContextAccessor,
        ITeamService teamService,
        IUserService userService,
        IScopeRegistry scopeRegistry = null)
    {
        _httpContextAccessor = httpContextAccessor;
        _teamService = teamService;
        _userService = userService;
        _scopeRegistry = scopeRegistry;
    }

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity { IsAuthenticated: true } identity)
            return principal;

        // Re-entrance guard: if team claims are already present, skip
        if (identity.HasClaim(c => c.Type == Constants.TeamKeyCookie))
            return principal;

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
            return principal;

        if (!httpContext.Request.Cookies.TryGetValue(Constants.SelectedTeamKeyCookie, out var teamKey)
            || string.IsNullOrEmpty(teamKey))
            return principal;

        // Mark that we've processed this principal (re-entrance guard)
        identity.AddClaim(new Claim(Constants.TeamKeyCookie, teamKey));

        var user = await _userService.GetCurrentUserAsync(principal);
        var member = await _teamService.GetTeamMemberAsync(teamKey, user?.Key);
        if (member != null)
        {
            identity.AddClaim(new Claim(TeamClaimTypes.TeamKey, teamKey));
            identity.AddClaim(new Claim(ClaimTypes.Role, Roles.TeamMember));
            identity.AddClaim(new Claim(ClaimTypes.Role, $"Team{member.AccessLevel}"));
            identity.AddClaim(new Claim(TeamClaimTypes.AccessLevel, member.AccessLevel.ToString()));

            if (_scopeRegistry != null)
            {
                foreach (var scope in _scopeRegistry.GetEffectiveScopes(member.AccessLevel, member.TenantRoles, member.ScopeOverrides))
                {
                    identity.AddClaim(new Claim(TeamClaimTypes.Scope, scope));
                }
            }
        }

        return principal;
    }
}
