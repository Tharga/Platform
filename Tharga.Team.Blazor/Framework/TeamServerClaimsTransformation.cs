using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

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
    private readonly ITeamClaimsEnricher _claimsEnricher;
    private readonly ThargaBlazorOptions _options;

    public TeamServerClaimsTransformation(
        IHttpContextAccessor httpContextAccessor,
        ITeamService teamService,
        IUserService userService,
        IOptions<ThargaBlazorOptions> options,
        IScopeRegistry scopeRegistry = null,
        ITeamClaimsEnricher claimsEnricher = null)
    {
        _httpContextAccessor = httpContextAccessor;
        _teamService = teamService;
        _userService = userService;
        _options = options.Value;
        _scopeRegistry = scopeRegistry;
        _claimsEnricher = claimsEnricher;
    }

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity { IsAuthenticated: true } identity)
            return principal;

        // Re-entrance guard: if team claims are already present, skip
        if (identity.HasClaim(c => c.Type == Constants.TeamKeyCookie))
            return principal;

        // Run custom claims enricher before member lookup and consent evaluation
        if (_claimsEnricher != null)
        {
            await _claimsEnricher.EnrichAsync(identity);
        }

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
            return principal;

        if (!httpContext.Request.Cookies.TryGetValue(Constants.SelectedTeamKeyCookie, out var teamKey)
            || string.IsNullOrEmpty(teamKey))
            return principal;

        // Mark that we've processed this principal (re-entrance guard)
        AddClaimSafe(identity, Constants.TeamKeyCookie, teamKey);

        var user = await _userService.GetCurrentUserAsync(principal);
        var member = await _teamService.GetTeamMemberAsync(teamKey, user?.Key);
        if (member != null)
        {
            AddClaimSafe(identity, TeamClaimTypes.TeamKey, teamKey);
            AddClaimSafe(identity, ClaimTypes.Role, Roles.TeamMember);
            AddClaimSafe(identity, ClaimTypes.Role, $"Team{member.AccessLevel}");
            AddClaimSafe(identity, TeamClaimTypes.AccessLevel, member.AccessLevel.ToString());

            if (_scopeRegistry != null)
            {
                foreach (var scope in _scopeRegistry.GetEffectiveScopes(member.AccessLevel, member.TenantRoles, member.ScopeOverrides))
                {
                    AddClaimSafe(identity, TeamClaimTypes.Scope, scope);
                }
            }
        }
        else
        {
            // Check consent-based access: user is not a member but may have a global role
            // that has been granted viewer access by this team
            var userRoles = principal.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value)
                .ToArray();

            if (userRoles.Length > 0)
            {
                var consentedTeam = await _teamService.GetConsentedTeamsAsync(userRoles)
                    .FirstOrDefaultAsync(t => t.Key == teamKey);

                if (consentedTeam != null)
                {
                    var consentLevel = _options.ConsentAccessLevel;
                    AddClaimSafe(identity, TeamClaimTypes.TeamKey, teamKey);
                    AddClaimSafe(identity, ClaimTypes.Role, Roles.TeamMember);
                    AddClaimSafe(identity, ClaimTypes.Role, $"Team{consentLevel}");
                    AddClaimSafe(identity, TeamClaimTypes.AccessLevel, consentLevel.ToString());

                    if (_scopeRegistry != null)
                    {
                        foreach (var scope in _scopeRegistry.GetEffectiveScopes(consentLevel, [], []))
                        {
                            AddClaimSafe(identity, TeamClaimTypes.Scope, scope);
                        }
                    }
                }
            }
        }

        return principal;
    }

    private static void AddClaimSafe(ClaimsIdentity identity, string type, string value)
    {
        if (!identity.HasClaim(c => c.Type == type && c.Value == value))
            identity.AddClaim(new Claim(type, value));
    }
}
