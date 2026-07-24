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
    private readonly TeamMembershipClaimsBuilder _membershipClaimsBuilder;
    private readonly ISystemRoleRegistry _systemRoleRegistry;
    private readonly ITeamClaimsEnricher _claimsEnricher;

    public TeamServerClaimsTransformation(
        IHttpContextAccessor httpContextAccessor,
        TeamMembershipClaimsBuilder membershipClaimsBuilder,
        ISystemRoleRegistry systemRoleRegistry = null,
        ITeamClaimsEnricher claimsEnricher = null)
    {
        _httpContextAccessor = httpContextAccessor;
        _membershipClaimsBuilder = membershipClaimsBuilder;
        _systemRoleRegistry = systemRoleRegistry;
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

        // System scopes: global capabilities granted by the user's app roles (team-independent — applied even
        // when no team is selected). Added as Scope claims so [RequireScope] works the same as for team scopes.
        if (_systemRoleRegistry != null)
        {
            var appRoles = identity.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToArray();
            foreach (var scope in _systemRoleRegistry.GetScopesForRoles(appRoles))
                AddClaimSafe(identity, TeamClaimTypes.Scope, scope);
        }

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
            return principal;

        if (!httpContext.Request.Cookies.TryGetValue(Constants.SelectedTeamKeyCookie, out var teamKey)
            || string.IsNullOrEmpty(teamKey))
            return principal;

        // Mark that we've processed this principal (re-entrance guard)
        AddClaimSafe(identity, Constants.TeamKeyCookie, teamKey);

        // Team membership / consent claims — shared with the in-circuit revalidator so the two cannot drift.
        foreach (var claim in await _membershipClaimsBuilder.BuildAsync(principal, teamKey))
            AddClaimSafe(identity, claim.Type, claim.Value);

        return principal;
    }

    private static void AddClaimSafe(ClaimsIdentity identity, string type, string value)
    {
        if (!identity.HasClaim(c => c.Type == type && c.Value == value))
            identity.AddClaim(new Claim(type, value));
    }
}
