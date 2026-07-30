using Microsoft.AspNetCore.Http;
using Tharga.Team;
using Tharga.Team.Service;

namespace Tharga.Team.Mcp;

/// <summary>
/// Default <see cref="IMcpScopeChecker"/> implementation backed by the current <see cref="HttpContext"/>.
/// </summary>
public sealed class McpScopeChecker : IMcpScopeChecker
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public McpScopeChecker(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
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

        var teamKey = user.FindFirst(TeamClaimTypes.TeamKey)?.Value;
        return TeamScopePolicy.HasTeamScope(user, scope, teamKey);
    }

    public void Require(string scope)
    {
        if (!Has(scope))
            throw new UnauthorizedAccessException($"Missing required scope '{scope}'.");
    }
}
