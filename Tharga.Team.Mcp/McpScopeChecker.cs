using Microsoft.AspNetCore.Http;
using Tharga.Mcp;
using Tharga.Team;
using Tharga.Team.Service;

namespace Tharga.Team.Mcp;

/// <summary>
/// Default <see cref="IMcpScopeChecker"/> implementation backed by the current <see cref="HttpContext"/>.
/// </summary>
public sealed class McpScopeChecker : IMcpScopeChecker
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IMcpContextAccessor _contextAccessor;

    public McpScopeChecker(IHttpContextAccessor httpContextAccessor, IMcpContextAccessor contextAccessor = null)
    {
        _httpContextAccessor = httpContextAccessor;
        _contextAccessor = contextAccessor;
    }

    /// <summary>
    /// Whether the caller holds <paramref name="scope"/>, by either provenance: a system grant (app role
    /// or system API key) authorizes anywhere; a team grant authorizes within the caller's selected team.
    /// </summary>
    /// <remarks>
    /// Both are read because <c>AddTeam</c> registers the built-in <c>mcp:*</c> scopes into both
    /// registries. Reading only <c>SystemScope</c> made an access-level grant unsatisfiable — the scope
    /// was registered at <see cref="AccessLevel.Viewer"/> and emitted as a <c>Scope</c> claim that nothing
    /// then consulted.
    /// <para>
    /// Delegated to <c>TeamScopePolicy</c> rather than inspecting claims here: a team scope means "held
    /// for the selected team", and restating that rule per call site is how the enforcement paths came to
    /// disagree in the first place.
    /// </para>
    /// </remarks>
    public bool Has(string scope)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null) return false;

        if (TeamScopePolicy.HasSystemScope(user, scope)) return true;

        // A team named on this call is answered from the scopes resolved for *that* team, never from the
        // caller's claims. The principal carries scope claims for the team it is anchored to, so falling
        // through to them would answer a question about one team using another team's access -- which is
        // the whole hazard the selection has to avoid.
        if (_contextAccessor?.Current is TeamMcpContext { SelectedTeamScopes: not null } selected)
            return selected.SelectedTeamScopes.Contains(scope);

        var teamKey = user.FindFirst(TeamClaimTypes.TeamKey)?.Value;
        return TeamScopePolicy.HasTeamScope(user, scope, teamKey);
    }

    public void Require(string scope)
    {
        if (!Has(scope))
            throw new UnauthorizedAccessException($"Missing required scope '{scope}'.");
    }
}
