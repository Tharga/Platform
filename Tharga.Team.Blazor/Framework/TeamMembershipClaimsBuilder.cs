using System.Security.Claims;
using Microsoft.Extensions.Options;
using Tharga.Team;

namespace Tharga.Team.Blazor.Framework;

/// <summary>
/// Computes the team-membership/consent claims for a caller in a given team: team key, the
/// <see cref="Roles.TeamMember"/> and <c>Team{AccessLevel}</c> roles, access level, member key, and the
/// effective team scopes. Shared by <see cref="TeamServerClaimsTransformation"/> (initial HTTP
/// enrichment) and <see cref="TeamClaimRevalidator"/> (periodic in-circuit revalidation) so the two
/// server paths cannot drift.
/// </summary>
/// <remarks>
/// Returns only the team-derived claims — not the team-independent system scopes (those come from the
/// caller's app roles and are added separately by the transformation). Returns an empty list when the
/// caller has no membership and no consent access to the team.
/// </remarks>
internal sealed class TeamMembershipClaimsBuilder
{
    private readonly ITeamService _teamService;
    private readonly IUserService _userService;
    private readonly IScopeRegistry _scopeRegistry;
    private readonly ITenantRoleService _tenantRoleService;
    private readonly ThargaBlazorOptions _options;

    public TeamMembershipClaimsBuilder(
        ITeamService teamService,
        IUserService userService,
        IOptions<ThargaBlazorOptions> options,
        IScopeRegistry scopeRegistry = null,
        ITenantRoleService tenantRoleService = null)
    {
        _teamService = teamService;
        _userService = userService;
        _options = options.Value;
        _scopeRegistry = scopeRegistry;
        _tenantRoleService = tenantRoleService;
    }

    public async Task<IReadOnlyList<Claim>> BuildAsync(ClaimsPrincipal principal, string teamKey)
    {
        var claims = new List<Claim>();
        if (string.IsNullOrEmpty(teamKey))
            return claims;

        var user = await _userService.GetCurrentUserAsync(principal);
        var member = await _teamService.GetTeamMemberAsync(teamKey, user?.Key);

        if (member != null)
        {
            claims.Add(new Claim(TeamClaimTypes.TeamKey, teamKey));
            claims.Add(new Claim(ClaimTypes.Role, Roles.TeamMember));
            claims.Add(new Claim(ClaimTypes.Role, $"Team{member.AccessLevel}"));
            claims.Add(new Claim(TeamClaimTypes.AccessLevel, member.AccessLevel.ToString()));
            if (!string.IsNullOrEmpty(member.Key))
                claims.Add(new Claim(TeamClaimTypes.MemberKey, member.Key));

            var scopes = _tenantRoleService != null
                ? await _tenantRoleService.GetEffectiveScopesAsync(teamKey, member.AccessLevel, member.TenantRoles, member.ScopeOverrides)
                : _scopeRegistry?.GetEffectiveScopes(member.AccessLevel, member.TenantRoles, member.ScopeOverrides) ?? [];
            foreach (var scope in scopes)
                claims.Add(new Claim(TeamClaimTypes.Scope, scope));

            return claims;
        }

        // Not a member — the caller may still have consent-based access via a global role the team granted.
        var userRoles = principal.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .ToArray();

        if (userRoles.Length == 0)
            return claims;

        var consentedTeam = await _teamService.GetConsentedTeamsAsync(userRoles)
            .FirstOrDefaultAsync(t => t.Key == teamKey);

        if (consentedTeam == null)
            return claims;

        var consentLevel = consentedTeam.ConsentAccessLevel ?? _options.Consent.AccessLevel;
        claims.Add(new Claim(TeamClaimTypes.TeamKey, teamKey));
        claims.Add(new Claim(ClaimTypes.Role, Roles.TeamMember));
        claims.Add(new Claim(ClaimTypes.Role, $"Team{consentLevel}"));
        claims.Add(new Claim(TeamClaimTypes.AccessLevel, consentLevel.ToString()));

        if (_scopeRegistry != null)
        {
            foreach (var scope in _scopeRegistry.GetEffectiveScopes(consentLevel, [], []))
                claims.Add(new Claim(TeamClaimTypes.Scope, scope));
        }

        return claims;
    }
}
